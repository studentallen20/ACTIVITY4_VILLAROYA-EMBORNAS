using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Application.Services;

public class GetStudentBorrowingHistoryService
{
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IStudentRepository _studentRepo;

    public GetStudentBorrowingHistoryService(
        IBorrowingRepository borrowingRepo,
        IStudentRepository studentRepo)
    {
        _borrowingRepo = borrowingRepo;
        _studentRepo = studentRepo;
    }

    public async Task<OperationResult<IReadOnlyList<BorrowingDetailDto>>> ExecuteAsync(
        int studentId,
        CancellationToken ct = default)
    {
        var student = await _studentRepo.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            return OperationResult<IReadOnlyList<BorrowingDetailDto>>.Fail($"Student with ID {studentId} was not found.");
        }

        var history = await _borrowingRepo.GetHistoryByStudentIdAsync(studentId, ct);
        return OperationResult<IReadOnlyList<BorrowingDetailDto>>.Ok(
            history,
            $"Loaded {history.Count} borrowing record(s) for {student.Name}.");
    }
}
