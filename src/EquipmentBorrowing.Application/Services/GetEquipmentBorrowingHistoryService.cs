using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Application.Services;

public class GetEquipmentBorrowingHistoryService
{
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IEquipmentRepository _equipmentRepo;

    public GetEquipmentBorrowingHistoryService(
        IBorrowingRepository borrowingRepo,
        IEquipmentRepository equipmentRepo)
    {
        _borrowingRepo = borrowingRepo;
        _equipmentRepo = equipmentRepo;
    }

    public async Task<OperationResult<IReadOnlyList<BorrowingDetailDto>>> ExecuteAsync(
        int equipmentId,
        CancellationToken ct = default)
    {
        var equipment = await _equipmentRepo.GetByIdAsync(equipmentId, ct);
        if (equipment == null)
        {
            return OperationResult<IReadOnlyList<BorrowingDetailDto>>.Fail($"Equipment with ID {equipmentId} was not found.");
        }

        var history = await _borrowingRepo.GetHistoryByEquipmentIdAsync(equipmentId, ct);
        return OperationResult<IReadOnlyList<BorrowingDetailDto>>.Ok(
            history,
            $"Loaded {history.Count} borrowing record(s) for {equipment.Name} ({equipment.Identifier}).");
    }
}
