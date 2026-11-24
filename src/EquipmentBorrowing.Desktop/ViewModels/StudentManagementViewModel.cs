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

public partial class StudentManagementViewModel : ViewModelBase
{
    private readonly IStudentRepository _studentRepo;
    private readonly RegisterStudentService _registerService;
    private readonly UpdateStudentService _updateService;
    private readonly ChangeStudentEligibilityService _eligibilityService;

    [ObservableProperty]
    private ObservableCollection<Student> _studentsList = new();

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    // Register Form Fields (UC-12)
    [ObservableProperty]
    private string _newStudentNumber = string.Empty;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string? _newContactInfo;

    // Edit Form Fields (UC-13)
    [ObservableProperty]
    private string _editStudentNumber = string.Empty;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string? _editContactInfo;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    public StudentManagementViewModel(
        IStudentRepository studentRepo,
        RegisterStudentService registerService,
        UpdateStudentService updateService,
        ChangeStudentEligibilityService eligibilityService)
    {
        _studentRepo = studentRepo;
        _registerService = registerService;
        _updateService = updateService;
        _eligibilityService = eligibilityService;
    }

    [RelayCommand]
    public async Task LoadStudentsAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var students = await _studentRepo.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                string q = SearchQuery.Trim().ToLower();
                students = students
                    .Where(s => s.Name.ToLower().Contains(q) || s.StudentNumber.ToLower().Contains(q))
                    .ToList();
            }

            StudentsList = new ObservableCollection<Student>(students);
            if (!StudentsList.Any())
            {
                StatusMessage = "No student records found.";
                IsError = false;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load students: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterStudentAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _registerService.ExecuteAsync(NewStudentNumber, NewName, NewContactInfo);
            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                NewStudentNumber = string.Empty;
                NewName = string.Empty;
                NewContactInfo = null;
                await LoadStudentsAsync();
            }
            else
            {
                IsError = true;
                StatusMessage = string.Join(" ", result.Errors.Prepend(result.Message));
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Registration error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void StartEdit()
    {
        if (SelectedStudent == null)
        {
            IsError = true;
            StatusMessage = "Please select a student to edit.";
            return;
        }

        EditStudentNumber = SelectedStudent.StudentNumber;
        EditName = SelectedStudent.Name;
        EditContactInfo = SelectedStudent.ContactInfo;
        IsEditing = true;
        StatusMessage = null;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        StatusMessage = null;
    }

    [RelayCommand]
    public async Task SaveEditAsync()
    {
        if (SelectedStudent == null) return;

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _updateService.ExecuteAsync(
                SelectedStudent.Id,
                EditStudentNumber,
                EditName,
                EditContactInfo);

            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                IsEditing = false;
                await LoadStudentsAsync();
            }
            else
            {
                IsError = true;
                StatusMessage = string.Join(" ", result.Errors.Prepend(result.Message));
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ToggleEligibilityAsync()
    {
        if (SelectedStudent == null)
        {
            IsError = true;
            StatusMessage = "Please select a student to toggle borrowing eligibility.";
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            // Toggle to opposite of current eligibility
            bool newStatus = !SelectedStudent.IsEligibleToBorrow;
            var result = await _eligibilityService.ExecuteAsync(SelectedStudent.Id, newStatus);

            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                await LoadStudentsAsync();
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
            StatusMessage = $"Eligibility update error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
