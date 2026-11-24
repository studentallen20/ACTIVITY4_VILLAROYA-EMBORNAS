using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ChangeStudentEligibilityService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeStudentEligibilityService(IStudentRepository studentRepo, IUnitOfWork unitOfWork)
    {
        _studentRepo = studentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<Student>> ExecuteAsync(
        int studentId,
        bool makeEligible,
        CancellationToken ct = default)
    {
        var student = await _studentRepo.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            return OperationResult<Student>.Fail($"Student with ID {studentId} was not found.");
        }

        // BR-04: Repeating current choice causes no unintended change and gives understandable feedback
        if (student.IsEligibleToBorrow == makeEligible)
        {
            string status = makeEligible ? "eligible" : "restricted";
            return OperationResult<Student>.Ok(
                student,
                $"Student '{student.Name}' is already marked as {status}. No changes were made.");
        }

        // BR-04: Restriction prevents new borrowing, but does not cancel or prevent return of active borrowings
        student.SetEligibility(makeEligible);
        await _studentRepo.UpdateAsync(student, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        string actionText = makeEligible ? "permitted to borrow equipment" : "restricted from borrowing";
        return OperationResult<Student>.Ok(
            student,
            $"Student '{student.Name}' is now {actionText}. Existing active borrowings remain intact.");
    }
}
