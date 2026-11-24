using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class RegisterStudentService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterStudentService(IStudentRepository studentRepo, IUnitOfWork unitOfWork)
    {
        _studentRepo = studentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<Student>> ExecuteAsync(
        string studentNumber,
        string name,
        string? contactInfo,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // BR-01: Institutional student number required, trimmed, case-insensitive comparison
        if (string.IsNullOrWhiteSpace(studentNumber))
        {
            errors.Add("Student number is required.");
        }

        // BR-02: Nonblank full name required
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Student full name is required.");
        }

        if (errors.Any())
        {
            return OperationResult<Student>.Fail("Validation failed.", errors);
        }

        string normalizedNumber = Student.NormalizeStudentNumber(studentNumber);

        // BR-01 & BR-16: Enforce student number uniqueness
        bool exists = await _studentRepo.ExistsByStudentNumberAsync(normalizedNumber, null, ct);
        if (exists)
        {
            return OperationResult<Student>.Fail($"Student number '{normalizedNumber}' is already registered.");
        }

        // BR-02: Optional contact cleared if blank; newly registered student is eligible with default limit 3
        var student = new Student(normalizedNumber, name, contactInfo, isEligibleToBorrow: true, maxBorrowLimit: 3);

        await _studentRepo.AddAsync(student, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<Student>.Ok(student, $"Student '{student.Name}' ({student.StudentNumber}) registered successfully.");
    }
}
