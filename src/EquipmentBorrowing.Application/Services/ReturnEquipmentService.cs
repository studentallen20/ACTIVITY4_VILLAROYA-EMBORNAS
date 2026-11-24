using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepo,
        IEquipmentRepository equipmentRepo,
        IUnitOfWork unitOfWork)
    {
        _borrowingRepo = borrowingRepo;
        _equipmentRepo = equipmentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnResult> ExecuteAsync(int borrowingId, CancellationToken ct = default)
    {
        var borrowing = await _borrowingRepo.GetByIdAsync(borrowingId, ct);
        if (borrowing == null)
        {
            return ReturnResult.Fail($"Borrowing record with ID {borrowingId} was not found.");
        }

        // BR-13: Only active borrowings may be returned. Repeat return rejected without changing data.
        if (borrowing.Status == BorrowingStatus.Returned)
        {
            return ReturnResult.Fail($"Borrowing #{borrowingId} has already been returned previously.");
        }

        var equipment = await _equipmentRepo.GetByIdAsync(borrowing.EquipmentId, ct);
        if (equipment == null)
        {
            return ReturnResult.Fail($"Equipment for borrowing #{borrowingId} was not found.");
        }

        await using var tx = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // BR-13: Return transition: record actual return date, mark record returned, make item available
            var returnTime = DateTime.UtcNow;
            borrowing.MarkAsReturned(returnTime);
            await _borrowingRepo.UpdateAsync(borrowing, ct);

            equipment.MarkAsReturned();
            await _equipmentRepo.UpdateAsync(equipment, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ReturnResult.Ok(borrowing, $"Successfully returned '{equipment.Name}' ({equipment.Identifier}). Returned on {returnTime.ToLocalTime():yyyy-MM-dd}.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return ReturnResult.Fail($"Database failure during return: {ex.Message}");
        }
    }
}
