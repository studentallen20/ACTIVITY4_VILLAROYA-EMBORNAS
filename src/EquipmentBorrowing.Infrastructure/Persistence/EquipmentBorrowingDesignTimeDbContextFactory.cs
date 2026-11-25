using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();

        // Fallback design-time connection string for migrations tooling
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=desktop_app_dev;Username=postgres;Password=admin143";

        optionsBuilder.UseNpgsql(connectionString, b =>
        {
            b.MigrationsAssembly(typeof(EquipmentBorrowingDbContext).Assembly.FullName);
        });

        return new EquipmentBorrowingDbContext(optionsBuilder.Options);
    }
}
