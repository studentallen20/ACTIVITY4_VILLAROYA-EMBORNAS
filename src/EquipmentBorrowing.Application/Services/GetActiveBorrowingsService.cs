using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Application.Services;

public class GetActiveBorrowingsService
{
    private readonly IBorrowingRepository _borrowingRepo;

    public GetActiveBorrowingsService(IBorrowingRepository borrowingRepo)
    {
        _borrowingRepo = borrowingRepo;
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> ExecuteAsync(CancellationToken ct = default)
    {
        return await _borrowingRepo.GetActiveBorrowingsAsync(ct);
    }
}
