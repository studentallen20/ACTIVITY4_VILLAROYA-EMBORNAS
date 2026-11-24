using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        // 1. Build application configuration
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = configBuilder.Build();

        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=postgres";

        // 2. Configure Dependency Injection Composition Root
        var services = new ServiceCollection();

        // Database Context (Transient lifetime prevents concurrent multi-thread EF Core collisions during async UI calls)
        services.AddDbContext<EquipmentBorrowingDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        }, ServiceLifetime.Transient);

        // Repositories & Unit of Work
        services.AddTransient<IStudentRepository, EfStudentRepository>();
        services.AddTransient<IEquipmentRepository, EfEquipmentRepository>();
        services.AddTransient<IBorrowingRepository, EfBorrowingRepository>();
        services.AddTransient<IUnitOfWork, EfUnitOfWork>();

        // Application Services (14 use cases)
        services.AddTransient<GetAllEquipmentService>();
        services.AddTransient<GetAvailableEquipmentService>();
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<GetActiveBorrowingsService>();
        services.AddTransient<ReturnEquipmentService>();
        services.AddTransient<CheckOutMultipleEquipmentService>();
        services.AddTransient<GetStudentBorrowingHistoryService>();
        services.AddTransient<GetEquipmentBorrowingHistoryService>();
        services.AddTransient<SearchBorrowingsService>();
        services.AddTransient<RegisterStudentService>();
        services.AddTransient<UpdateStudentService>();
        services.AddTransient<ChangeStudentEligibilityService>();
        services.AddTransient<RegisterEquipmentService>();
        services.AddTransient<UpdateEquipmentService>();

        // ViewModels
        services.AddTransient<EquipmentCatalogViewModel>();
        services.AddTransient<ActiveBorrowingsViewModel>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<BorrowingHistoryViewModel>();
        services.AddTransient<StudentManagementViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // 3. Graceful asynchronous database migration & seeding
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
                await DatabaseInitializer.InitializeAsync(dbContext);
                mainWindowViewModel.UpdateDatabaseStatus(true, "PostgreSQL Database Online (equipment_borrowing)");
            }
            catch (Exception ex)
            {
                mainWindowViewModel.UpdateDatabaseStatus(false, $"Database Offline / Authentication needed: {ex.Message}");
            }

            // Initial load of equipment catalog
            await mainWindowViewModel.EquipmentCatalogViewModel.LoadEquipmentAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
