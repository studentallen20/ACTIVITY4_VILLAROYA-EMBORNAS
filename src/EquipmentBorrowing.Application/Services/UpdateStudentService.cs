using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class UpdateStudentService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStudentService(IStudentRepository studentRepo, IUnitOfWork unitOfWork)
    {
        _studentRepo = studentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<Student>> ExecuteAsync(
        int studentId,
        string studentNumber,
        string name,
        string? contactInfo,
        CancellationToken ct = default)
    {
        var student = await _studentRepo.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            return OperationResult<Student>.Fail($"Student with ID {studentId} was not found.");
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(studentNumber))
        {
            errors.Add("Student number cannot be blank.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Student name cannot be blank.");
        }

        if (errors.Any())
        {
            return OperationResult<Student>.Fail("Validation failed.", errors);
        }

        string normalizedNumber = Student.NormalizeStudentNumber(studentNumber);

        // BR-01, BR-03: If student number changed, ensure uniqueness
        bool duplicate = await _studentRepo.ExistsByStudentNumberAsync(normalizedNumber, studentId, ct);
        if (duplicate)
        {
            return OperationResult<Student>.Fail($"Student number '{normalizedNumber}' is already in use by another student.");
        }

        // BR-03: Update descriptive fields, clear contact if blank, preserve ID and borrowing history
        student.UpdateInformation(normalizedNumber, name, contactInfo);

        await _studentRepo.UpdateAsync(student, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<Student>.Ok(student, $"Student #{studentId} updated successfully.");
    }
}
