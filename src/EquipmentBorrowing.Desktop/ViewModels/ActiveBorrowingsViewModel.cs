using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class ActiveBorrowingsViewModel : ViewModelBase
{
    private readonly GetActiveBorrowingsService _getActiveService;
    private readonly ReturnEquipmentService _returnService;

    [ObservableProperty]
    private ObservableCollection<BorrowingDetailDto> _activeBorrowings = new();

    [ObservableProperty]
    private BorrowingDetailDto? _selectedBorrowing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isError;

    public ActiveBorrowingsViewModel(
        GetActiveBorrowingsService getActiveService,
        ReturnEquipmentService returnService)
    {
        _getActiveService = getActiveService;
        _returnService = returnService;
    }

    [RelayCommand]
    public async Task LoadActiveBorrowingsAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var items = await _getActiveService.ExecuteAsync();
            ActiveBorrowings = new ObservableCollection<BorrowingDetailDto>(items);

            if (!ActiveBorrowings.Any())
            {
                StatusMessage = "No active borrowings recorded. All equipment items are accounted for.";
                IsError = false;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load active borrowings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ReturnSelectedEquipmentAsync()
    {
        if (SelectedBorrowing == null)
        {
            IsError = true;
            StatusMessage = "Please select an active borrowing to return.";
            return;
        }

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var result = await _returnService.ExecuteAsync(SelectedBorrowing.Id);
            if (result.Success)
            {
                IsError = false;
                StatusMessage = result.Message;
                await LoadActiveBorrowingsAsync();
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
            StatusMessage = $"Return failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
