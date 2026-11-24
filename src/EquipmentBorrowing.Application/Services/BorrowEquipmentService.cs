using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public BorrowEquipmentService(
        IStudentRepository studentRepo,
        IEquipmentRepository equipmentRepo,
        IBorrowingRepository borrowingRepo,
        IUnitOfWork unitOfWork)
    {
        _studentRepo = studentRepo;
        _equipmentRepo = equipmentRepo;
        _borrowingRepo = borrowingRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<BorrowResult> ExecuteAsync(
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken ct = default)
    {
        // BR-11: Expected return date must be later than checkout date (using local date comparison)
        var today = DateTime.UtcNow.Date;
        if (expectedReturnDate.Date <= today)
        {
            return BorrowResult.Fail($"Expected return date ({expectedReturnDate:yyyy-MM-dd}) must be later than today ({today:yyyy-MM-dd}).");
        }

        // BR-08: Revalidate student exists and is eligible
        var student = await _studentRepo.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            return BorrowResult.Fail($"Student with ID {studentId} was not found.");
        }

        if (!student.IsEligibleToBorrow)
        {
            return BorrowResult.Fail($"Student '{student.Name}' ({student.StudentNumber}) is currently restricted from borrowing.");
        }

        // BR-09: Active limit check (max 3 items)
        int activeCount = await _borrowingRepo.GetActiveBorrowingCountAsync(studentId, ct);
        if (activeCount >= student.MaxBorrowLimit)
        {
            return BorrowResult.Fail($"Student '{student.Name}' already has {activeCount} active borrowing(s) (Maximum allowed is {student.MaxBorrowLimit}).");
        }

        // BR-08 & BR-10: Revalidate equipment exists and is available
        var equipment = await _equipmentRepo.GetByIdAsync(equipmentId, ct);
        if (equipment == null)
        {
            return BorrowResult.Fail($"Equipment with ID {equipmentId} was not found.");
        }

        if (!equipment.IsAvailable)
        {
            return BorrowResult.Fail($"Equipment '{equipment.Name}' ({equipment.Identifier}) is currently unavailable.");
        }

        // Check if there is already an active borrowing for this equipment
        var existingActive = await _borrowingRepo.GetActiveBorrowingByEquipmentIdAsync(equipmentId, ct);
        if (existingActive != null)
        {
            return BorrowResult.Fail($"Equipment '{equipment.Name}' ({equipment.Identifier}) already has an active borrowing.");
        }

        // BR-15: Atomic state transition
        await using var tx = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var borrowing = new Borrowing(studentId, equipmentId, DateTime.UtcNow, expectedReturnDate);
            await _borrowingRepo.AddAsync(borrowing, ct);

            equipment.MarkAsBorrowed();
            await _equipmentRepo.UpdateAsync(equipment, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return BorrowResult.Ok(borrowing, $"Successfully checked out '{equipment.Name}' to {student.Name}. Due on {borrowing.DueDate:yyyy-MM-dd}.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return BorrowResult.Fail($"Database failure during checkout: {ex.Message}");
        }
    }
}
