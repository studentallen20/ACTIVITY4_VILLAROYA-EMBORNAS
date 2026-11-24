using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Models;

public class ReturnResult
{
    public bool Success { get; }
    public string Message { get; }
    public Borrowing? Borrowing { get; }

    private ReturnResult(bool success, string message, Borrowing? borrowing = null)
    {
        Success = success;
        Message = message;
        Borrowing = borrowing;
    }

    public static ReturnResult Ok(Borrowing borrowing, string message) => new(true, message, borrowing);
    public static ReturnResult Fail(string message) => new(false, message);
}
