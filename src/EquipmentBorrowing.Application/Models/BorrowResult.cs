using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Models;

public class BorrowResult
{
    public bool Success { get; }
    public string Message { get; }
    public Borrowing? Borrowing { get; }

    private BorrowResult(bool success, string message, Borrowing? borrowing = null)
    {
        Success = success;
        Message = message;
        Borrowing = borrowing;
    }

    public static BorrowResult Ok(Borrowing borrowing, string message) => new(true, message, borrowing);
    public static BorrowResult Fail(string message) => new(false, message);
}
