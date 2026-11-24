using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingHistoryViewModel : ViewModelBase
{
    private readonly SearchBorrowingsService _searchService;
    private readonly GetStudentBorrowingHistoryService _studentHistoryService;
    private readonly GetEquipmentBorrowingHistoryService _equipmentHistoryService;
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;

    // Search & Filter (UC-09)
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusFilter = "All"; // "All", "Active", "Returned"

    [ObservableProperty]
    private string _sortField = "BorrowDate"; // "BorrowDate", "DueDate"

    [ObservableProperty]
    private bool _sortAscending = false;

    [ObservableProperty]
    private ObservableCollection<BorrowingDetailDto> _searchResults = new();

    // Student History (UC-07)
    [ObservableProperty]
    private ObservableCollection<Student> _studentsList = new();

    [ObservableProperty]
    private Student? _selectedHistoryStudent;

    [ObservableProperty]
    private ObservableCollection<BorrowingDetailDto> _studentHistoryList = new();

    // Equipment History (UC-08)
    [ObservableProperty]
    private ObservableCollection<Equipment> _equipmentList = new();

    [ObservableProperty]
    private Equipment? _selectedHistoryEquipment;

    [ObservableProperty]
    private ObservableCollection<BorrowingDetailDto> _equipmentHistoryList = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    public BorrowingHistoryViewModel(
        SearchBorrowingsService searchService,
        GetStudentBorrowingHistoryService studentHistoryService,
        GetEquipmentBorrowingHistoryService equipmentHistoryService,
        IStudentRepository studentRepo,
        IEquipmentRepository equipmentRepo)
    {
        _searchService = searchService;
        _studentHistoryService = studentHistoryService;
        _equipmentHistoryService = equipmentHistoryService;
        _studentRepo = studentRepo;
        _equipmentRepo = equipmentRepo;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var students = await _studentRepo.GetAllAsync();
            StudentsList = new ObservableCollection<Student>(students);

            var equipment = await _equipmentRepo.GetAllAsync();
            EquipmentList = new ObservableCollection<Equipment>(equipment);

            await ExecuteSearchAsync();
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to initialize history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExecuteSearchAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            BorrowingStatus? status = StatusFilter switch
            {
                "Active" => BorrowingStatus.Active,
                "Returned" => BorrowingStatus.Returned,
                _ => null
            };

            var items = await _searchService.ExecuteAsync(SearchQuery, status, SortField, SortAscending);
            SearchResults = new ObservableCollection<BorrowingDetailDto>(items);

            if (!SearchResults.Any())
            {
                StatusMessage = "No borrowing records match your criteria.";
                IsError = false;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResetSearchAsync()
    {
        SearchQuery = string.Empty;
        StatusFilter = "All";
        SortField = "BorrowDate";
        SortAscending = false;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    public async Task LoadStudentHistoryAsync()
    {
        if (SelectedHistoryStudent == null)
        {
            StudentHistoryList.Clear();
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _studentHistoryService.ExecuteAsync(SelectedHistoryStudent.Id);
            if (result.Success && result.Data != null)
            {
                StudentHistoryList = new ObservableCollection<BorrowingDetailDto>(result.Data);
                if (!StudentHistoryList.Any())
                {
                    StatusMessage = $"No borrowing records found for {SelectedHistoryStudent.Name}.";
                    IsError = false;
                }
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
            StatusMessage = $"Error loading student history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadEquipmentHistoryAsync()
    {
        if (SelectedHistoryEquipment == null)
        {
            EquipmentHistoryList.Clear();
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _equipmentHistoryService.ExecuteAsync(SelectedHistoryEquipment.Id);
            if (result.Success && result.Data != null)
            {
                EquipmentHistoryList = new ObservableCollection<BorrowingDetailDto>(result.Data);
                if (!EquipmentHistoryList.Any())
                {
                    StatusMessage = $"No borrowing history found for {SelectedHistoryEquipment.Name}.";
                    IsError = false;
                }
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
            StatusMessage = $"Error loading equipment history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedHistoryStudentChanged(Student? value)
    {
        _ = LoadStudentHistoryAsync();
    }

    partial void OnSelectedHistoryEquipmentChanged(Equipment? value)
    {
        _ = LoadEquipmentHistoryAsync();
    }
}
