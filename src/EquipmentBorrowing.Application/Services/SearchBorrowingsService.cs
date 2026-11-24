using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class SearchBorrowingsService
{
    private readonly IBorrowingRepository _borrowingRepo;

    public SearchBorrowingsService(IBorrowingRepository borrowingRepo)
    {
        _borrowingRepo = borrowingRepo;
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> ExecuteAsync(
        string? searchText,
        BorrowingStatus? status,
        string? sortBy = "BorrowDate",
        bool ascending = false,
        CancellationToken ct = default)
    {
        // BR-17: Search by case-insensitive partial text in student name, student number, or equipment name.
        // Trim surrounding whitespace. Filter by active/returned/all. Sort with borrowing ID as stable tie-breaker.
        return await _borrowingRepo.SearchAndFilterAsync(searchText, status, sortBy, ascending, ct);
    }
}
