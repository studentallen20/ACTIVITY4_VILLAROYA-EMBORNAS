namespace EquipmentBorrowing.Application.Models;

public class OperationResult<T>
{
    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }
    public IReadOnlyList<string> Errors { get; }

    private OperationResult(bool success, string message, T? data = default, IReadOnlyList<string>? errors = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = errors ?? Array.Empty<string>();
    }

    public static OperationResult<T> Ok(T data, string message = "Operation completed successfully.") =>
        new(true, message, data);

    public static OperationResult<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new(false, message, default, errors);
}
