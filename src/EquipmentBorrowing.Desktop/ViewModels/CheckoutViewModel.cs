using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentSelectionItem : ObservableObject
{
    public Equipment Equipment { get; }

    [ObservableProperty]
    private bool _isSelected;

    public EquipmentSelectionItem(Equipment equipment, bool isSelected = false)
    {
        Equipment = equipment;
        _isSelected = isSelected;
    }
}

public partial class CheckoutViewModel : ViewModelBase
{
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly BorrowEquipmentService _singleBorrowService;
    private readonly CheckOutMultipleEquipmentService _multiBorrowService;

    [ObservableProperty]
    private ObservableCollection<Student> _students = new();

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private ObservableCollection<EquipmentSelectionItem> _availableEquipment = new();

    [ObservableProperty]
    private DateTimeOffset _expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private bool _simulateTransactionFailure;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _studentLoanInfo = string.Empty;

    public CheckoutViewModel(
        IStudentRepository studentRepo,
        IEquipmentRepository equipmentRepo,
        IBorrowingRepository borrowingRepo,
        BorrowEquipmentService singleBorrowService,
        CheckOutMultipleEquipmentService multiBorrowService)
    {
        _studentRepo = studentRepo;
        _equipmentRepo = equipmentRepo;
        _borrowingRepo = borrowingRepo;
        _singleBorrowService = singleBorrowService;
        _multiBorrowService = multiBorrowService;
    }

    [RelayCommand]
    public async Task LoadFormDataAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var allStudents = await _studentRepo.GetAllAsync();
            Students = new ObservableCollection<Student>(allStudents);

            var availableEq = await _equipmentRepo.GetAvailableAsync();
            AvailableEquipment = new ObservableCollection<EquipmentSelectionItem>(
                availableEq.Select(e => new EquipmentSelectionItem(e)));

            if (SelectedStudent != null)
            {
                await UpdateStudentLoanInfoAsync(SelectedStudent.Id);
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load checkout options: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task IssueSingleItemAsync()
    {
        if (SelectedStudent == null)
        {
            IsError = true;
            StatusMessage = "Please select a student.";
            return;
        }

        var selectedItems = AvailableEquipment.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count != 1)
        {
            IsError = true;
            StatusMessage = "Single-item checkout requires exactly 1 equipment item to be selected.";
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _singleBorrowService.ExecuteAsync(
                SelectedStudent.Id,
                selectedItems[0].Equipment.Id,
                ExpectedReturnDate.UtcDateTime);

            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                await LoadFormDataAsync();
            }
            else
            {
                IsError = true;
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Checkout error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task IssueMultiItemAsync()
    {
        if (SelectedStudent == null)
        {
            IsError = true;
            StatusMessage = "Please select a student.";
            return;
        }

        var selectedItems = AvailableEquipment.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count < 2)
        {
            IsError = true;
            StatusMessage = "Multi-item checkout (UC-06) requires at least two distinct equipment items.";
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var eqIds = selectedItems.Select(x => x.Equipment.Id).ToList();
            var result = await _multiBorrowService.ExecuteAsync(
                SelectedStudent.Id,
                eqIds,
                ExpectedReturnDate.UtcDateTime,
                simulateFailureAfterWrites: SimulateTransactionFailure);

            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                await LoadFormDataAsync();
            }
            else
            {
                IsError = true;
                StatusMessage = string.Join(" | ", result.Errors.Prepend(result.Message));
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Transaction failure: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value != null)
        {
            _ = UpdateStudentLoanInfoAsync(value.Id);
        }
        else
        {
            StudentLoanInfo = string.Empty;
        }
    }

    private async Task UpdateStudentLoanInfoAsync(int studentId)
    {
        try
        {
            int active = await _borrowingRepo.GetActiveBorrowingCountAsync(studentId);
            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student != null)
            {
                string eligibility = student.IsEligibleToBorrow ? "Eligible" : "RESTRICTED";
                StudentLoanInfo = $"Status: {eligibility} | Active Loans: {active} of {student.MaxBorrowLimit} max allowed";
            }
        }
        catch
        {
            StudentLoanInfo = string.Empty;
        }
    }
}
