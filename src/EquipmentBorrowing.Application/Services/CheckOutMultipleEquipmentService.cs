using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class CheckOutMultipleEquipmentService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CheckOutMultipleEquipmentService(
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

    public async Task<MultiBorrowResult> ExecuteAsync(
        int studentId,
        IReadOnlyList<int> equipmentIds,
        DateTime expectedReturnDate,
        bool simulateFailureAfterWrites = false,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // BR-12: Multi-item request requires at least two distinct items
        if (equipmentIds == null || equipmentIds.Count < 2)
        {
            errors.Add("Multi-item checkout requires at least two items to be selected.");
            return MultiBorrowResult.Fail("Invalid multi-item request.", errors);
        }

        // BR-12: Check for duplicate item selections
        var distinctIds = equipmentIds.Distinct().ToList();
        if (distinctIds.Count != equipmentIds.Count)
        {
            errors.Add("Duplicate equipment selections detected. Each requested item must be unique.");
            return MultiBorrowResult.Fail("Invalid multi-item request.", errors);
        }

        // BR-11: Expected return date must be later than checkout date
        var today = DateTime.UtcNow.Date;
        if (expectedReturnDate.Date <= today)
        {
            errors.Add($"Expected return date ({expectedReturnDate:yyyy-MM-dd}) must be later than today ({today:yyyy-MM-dd}).");
            return MultiBorrowResult.Fail("Invalid return date.", errors);
        }

        // BR-08: Revalidate student exists and is eligible
        var student = await _studentRepo.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            errors.Add($"Student with ID {studentId} was not found.");
            return MultiBorrowResult.Fail("Student not found.", errors);
        }

        if (!student.IsEligibleToBorrow)
        {
            errors.Add($"Student '{student.Name}' ({student.StudentNumber}) is currently restricted from borrowing.");
            return MultiBorrowResult.Fail("Student is ineligible.", errors);
        }

        // BR-09: Revalidate active loan limit (existing active + requested <= MaxBorrowLimit)
        int currentActive = await _borrowingRepo.GetActiveBorrowingCountAsync(studentId, ct);
        int totalProjected = currentActive + distinctIds.Count;
        if (totalProjected > student.MaxBorrowLimit)
        {
            errors.Add($"Student '{student.Name}' currently has {currentActive} active loan(s). Adding {distinctIds.Count} items would reach {totalProjected}, exceeding the limit of {student.MaxBorrowLimit}.");
            return MultiBorrowResult.Fail("Borrowing limit exceeded.", errors);
        }

        // BR-08 & BR-10: Revalidate each selected equipment exists and is available
        var equipmentList = new List<Equipment>();
        foreach (int eqId in distinctIds)
        {
            var eq = await _equipmentRepo.GetByIdAsync(eqId, ct);
            if (eq == null)
            {
                errors.Add($"Equipment with ID {eqId} was not found.");
                continue;
            }

            if (!eq.IsAvailable)
            {
                errors.Add($"Equipment '{eq.Name}' ({eq.Identifier}) is currently unavailable.");
            }

            var existingActive = await _borrowingRepo.GetActiveBorrowingByEquipmentIdAsync(eqId, ct);
            if (existingActive != null)
            {
                errors.Add($"Equipment '{eq.Name}' ({eq.Identifier}) already has an active borrowing.");
            }

            equipmentList.Add(eq);
        }

        if (errors.Any())
        {
            return MultiBorrowResult.Fail("One or more validation checks failed.", errors);
        }

        // BR-15: Atomicity guarantee: commit ALL or NONE within a single database transaction
        await using var tx = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var createdBorrowings = new List<Borrowing>();
            var now = DateTime.UtcNow;

            foreach (var eq in equipmentList)
            {
                var borrowing = new Borrowing(studentId, eq.Id, now, expectedReturnDate);
                createdBorrowings.Add(borrowing);
                await _borrowingRepo.AddAsync(borrowing, ct);

                eq.MarkAsBorrowed();
                await _equipmentRepo.UpdateAsync(eq, ct);
            }

            // Save partial writes to stage them in the active transaction
            await _unitOfWork.SaveChangesAsync(ct);

            // Controlled test seam for deliberate A06 failure verification
            if (simulateFailureAfterWrites)
            {
                throw new InvalidOperationException("Simulated mid-transaction failure triggered (A06 verification seam). All partial writes must roll back.");
            }

            await tx.CommitAsync(ct);

            return MultiBorrowResult.Ok(
                createdBorrowings,
                $"Successfully checked out {createdBorrowings.Count} items to {student.Name}. Due on {expectedReturnDate:yyyy-MM-dd}.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return MultiBorrowResult.Fail($"Transaction rolled back safely: {ex.Message}");
        }
    }
}
