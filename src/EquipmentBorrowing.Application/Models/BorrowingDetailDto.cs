using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Models;

public class BorrowingDetailDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EquipmentIdentifier { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BorrowingStatus Status { get; set; }
    public bool IsActive => Status == BorrowingStatus.Active;

    public string StatusText => Status.ToString();
    public string FormattedBorrowDate => BorrowDate.ToLocalTime().ToString("yyyy-MM-dd");
    public string FormattedDueDate => DueDate.ToLocalTime().ToString("yyyy-MM-dd");
    public string FormattedReturnDate => ReturnDate.HasValue ? ReturnDate.Value.ToLocalTime().ToString("yyyy-MM-dd") : "—";
}
