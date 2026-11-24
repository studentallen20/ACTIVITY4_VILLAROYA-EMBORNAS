namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public bool IsEligibleToBorrow { get; set; } = true;
    public string EligibilityStatus => IsEligibleToBorrow ? "Eligible" : "Restricted";
    public int MaxBorrowLimit { get; set; } = 3;

    public Student() { }

    public Student(string studentNumber, string name, string? contactInfo = null, bool isEligibleToBorrow = true, int maxBorrowLimit = 3)
    {
        StudentNumber = NormalizeStudentNumber(studentNumber);
        Name = name?.Trim() ?? string.Empty;
        ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();
        IsEligibleToBorrow = isEligibleToBorrow;
        MaxBorrowLimit = maxBorrowLimit;
    }

    public Student(int id, string studentNumber, string name, string? contactInfo = null, bool isEligibleToBorrow = true, int maxBorrowLimit = 3)
        : this(studentNumber, name, contactInfo, isEligibleToBorrow, maxBorrowLimit)
    {
        Id = id;
    }

    public static string NormalizeStudentNumber(string? studentNumber)
    {
        return studentNumber?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public bool CanBorrow(int currentActiveBorrowings, int requestedCount = 1)
    {
        return IsEligibleToBorrow && (currentActiveBorrowings + requestedCount) <= MaxBorrowLimit;
    }

    public void UpdateInformation(string studentNumber, string name, string? contactInfo)
    {
        StudentNumber = NormalizeStudentNumber(studentNumber);
        Name = name?.Trim() ?? string.Empty;
        ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();
    }

    public void SetEligibility(bool isEligible)
    {
        IsEligibleToBorrow = isEligible;
    }
}
