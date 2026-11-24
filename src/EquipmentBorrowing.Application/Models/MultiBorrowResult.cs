using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Models;

public class MultiBorrowResult
{
    public bool Success { get; }
    public string Message { get; }
    public IReadOnlyList<Borrowing> Borrowings { get; }
    public IReadOnlyList<string> Errors { get; }

    private MultiBorrowResult(bool success, string message, IReadOnlyList<Borrowing>? borrowings = null, IReadOnlyList<string>? errors = null)
    {
        Success = success;
        Message = message;
        Borrowings = borrowings ?? Array.Empty<Borrowing>();
        Errors = errors ?? Array.Empty<string>();
    }

    public static MultiBorrowResult Ok(IReadOnlyList<Borrowing> borrowings, string message) =>
        new(true, message, borrowings);

    public static MultiBorrowResult Fail(string message, IReadOnlyList<string>? errors = null) =>
        new(false, message, null, errors);
}
