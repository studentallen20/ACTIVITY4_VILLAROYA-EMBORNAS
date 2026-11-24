using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public EquipmentCatalogViewModel EquipmentCatalogViewModel { get; }
    public ActiveBorrowingsViewModel ActiveBorrowingsViewModel { get; }
    public CheckoutViewModel CheckoutViewModel { get; }
    public BorrowingHistoryViewModel BorrowingHistoryViewModel { get; }
    public StudentManagementViewModel StudentManagementViewModel { get; }

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private bool _isEquipmentActive = true;

    [ObservableProperty]
    private bool _isBorrowingsActive = false;

    [ObservableProperty]
    private bool _isCheckoutActive = false;

    [ObservableProperty]
    private bool _isHistoryActive = false;

    [ObservableProperty]
    private bool _isStudentsActive = false;

    [ObservableProperty]
    private string _databaseStatus = "Checking PostgreSQL connection...";

    [ObservableProperty]
    private bool _isDatabaseConnected = true;

    public MainWindowViewModel(
        EquipmentCatalogViewModel equipmentCatalogViewModel,
        ActiveBorrowingsViewModel activeBorrowingsViewModel,
        CheckoutViewModel checkoutViewModel,
        BorrowingHistoryViewModel borrowingHistoryViewModel,
        StudentManagementViewModel studentManagementViewModel)
    {
        EquipmentCatalogViewModel = equipmentCatalogViewModel;
        ActiveBorrowingsViewModel = activeBorrowingsViewModel;
        CheckoutViewModel = checkoutViewModel;
        BorrowingHistoryViewModel = borrowingHistoryViewModel;
        StudentManagementViewModel = studentManagementViewModel;

        _currentPage = equipmentCatalogViewModel;
    }

    [RelayCommand]
    public async Task NavigateToEquipmentAsync()
    {
        CurrentPage = EquipmentCatalogViewModel;
        SetNavState(equipment: true);
        await EquipmentCatalogViewModel.LoadEquipmentAsync();
    }

    [RelayCommand]
    public async Task NavigateToBorrowingsAsync()
    {
        CurrentPage = ActiveBorrowingsViewModel;
        SetNavState(borrowings: true);
        await ActiveBorrowingsViewModel.LoadActiveBorrowingsAsync();
    }

    [RelayCommand]
    public async Task NavigateToCheckoutAsync()
    {
        CurrentPage = CheckoutViewModel;
        SetNavState(checkout: true);
        await CheckoutViewModel.LoadFormDataAsync();
    }

    [RelayCommand]
    public async Task NavigateToHistoryAsync()
    {
        CurrentPage = BorrowingHistoryViewModel;
        SetNavState(history: true);
        await BorrowingHistoryViewModel.InitializeAsync();
    }

    [RelayCommand]
    public async Task NavigateToStudentsAsync()
    {
        CurrentPage = StudentManagementViewModel;
        SetNavState(students: true);
        await StudentManagementViewModel.LoadStudentsAsync();
    }

    private void SetNavState(
        bool equipment = false,
        bool borrowings = false,
        bool checkout = false,
        bool history = false,
        bool students = false)
    {
        IsEquipmentActive = equipment;
        IsBorrowingsActive = borrowings;
        IsCheckoutActive = checkout;
        IsHistoryActive = history;
        IsStudentsActive = students;
    }

    public void UpdateDatabaseStatus(bool connected, string message)
    {
        IsDatabaseConnected = connected;
        DatabaseStatus = message;
    }
}
