using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task<Borrowing?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> GetActiveBorrowingCountAsync(int studentId, CancellationToken ct = default);
    Task<Borrowing?> GetActiveBorrowingByEquipmentIdAsync(int equipmentId, CancellationToken ct = default);
    Task<IReadOnlyList<BorrowingDetailDto>> GetActiveBorrowingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BorrowingDetailDto>> GetHistoryByStudentIdAsync(int studentId, CancellationToken ct = default);
    Task<IReadOnlyList<BorrowingDetailDto>> GetHistoryByEquipmentIdAsync(int equipmentId, CancellationToken ct = default);
    Task<IReadOnlyList<BorrowingDetailDto>> SearchAndFilterAsync(
        string? searchText,
        BorrowingStatus? status,
        string? sortBy,
        bool ascending,
        CancellationToken ct = default);
    Task AddAsync(Borrowing borrowing, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Borrowing> borrowings, CancellationToken ct = default);
    Task UpdateAsync(Borrowing borrowing, CancellationToken ct = default);
}
