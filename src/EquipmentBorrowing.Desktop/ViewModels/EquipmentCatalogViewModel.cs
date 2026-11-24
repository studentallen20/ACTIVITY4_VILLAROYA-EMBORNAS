using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentCatalogViewModel : ViewModelBase
{
    private readonly GetAllEquipmentService _getAllService;
    private readonly GetAvailableEquipmentService _getAvailableService;
    private readonly RegisterEquipmentService _registerService;
    private readonly UpdateEquipmentService _updateService;

    [ObservableProperty]
    private ObservableCollection<Equipment> _equipmentList = new();

    [ObservableProperty]
    private Equipment? _selectedEquipment;

    [ObservableProperty]
    private bool _showAvailableOnly;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    // Register Form Fields (UC-15)
    [ObservableProperty]
    private string _newIdentifier = string.Empty;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string? _newDescription;

    // Edit Form Fields (UC-16)
    [ObservableProperty]
    private string _editIdentifier = string.Empty;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string? _editDescription;

    [ObservableProperty]
    private bool _isEditing;

    public EquipmentCatalogViewModel(
        GetAllEquipmentService getAllService,
        GetAvailableEquipmentService getAvailableService,
        RegisterEquipmentService registerService,
        UpdateEquipmentService updateService)
    {
        _getAllService = getAllService;
        _getAvailableService = getAvailableService;
        _registerService = registerService;
        _updateService = updateService;
    }

    [RelayCommand]
    public async Task LoadEquipmentAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var items = ShowAvailableOnly
                ? await _getAvailableService.ExecuteAsync()
                : await _getAllService.ExecuteAsync();

            EquipmentList = new ObservableCollection<Equipment>(items);
            if (!EquipmentList.Any())
            {
                StatusMessage = ShowAvailableOnly
                    ? "No equipment is currently available."
                    : "No equipment records found in catalog.";
                IsError = false;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load equipment catalog: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterEquipmentAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _registerService.ExecuteAsync(NewIdentifier, NewName, NewDescription);
            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                NewIdentifier = string.Empty;
                NewName = string.Empty;
                NewDescription = null;
                await LoadEquipmentAsync();
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
        if (SelectedEquipment == null)
        {
            IsError = true;
            StatusMessage = "Please select an equipment item to edit.";
            return;
        }

        EditIdentifier = SelectedEquipment.Identifier;
        EditName = SelectedEquipment.Name;
        EditDescription = SelectedEquipment.Description;
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
        if (SelectedEquipment == null) return;

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _updateService.ExecuteAsync(
                SelectedEquipment.Id,
                EditIdentifier,
                EditName,
                EditDescription);

            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                IsEditing = false;
                await LoadEquipmentAsync();
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

    partial void OnShowAvailableOnlyChanged(bool value)
    {
        _ = LoadEquipmentAsync();
    }
}
