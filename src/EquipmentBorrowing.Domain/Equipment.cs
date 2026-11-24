namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string AvailabilityStatus => IsAvailable ? "Available" : "Borrowed";

    public Equipment() { }

    public Equipment(string identifier, string name, string? description = null, bool isAvailable = true)
    {
        Identifier = NormalizeIdentifier(identifier);
        Name = name?.Trim() ?? string.Empty;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsAvailable = isAvailable;
    }

    public Equipment(int id, string identifier, string name, string? description = null, bool isAvailable = true)
        : this(identifier, name, description, isAvailable)
    {
        Id = id;
    }

    public static string NormalizeIdentifier(string? identifier)
    {
        return identifier?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public void UpdateDescriptiveInfo(string identifier, string name, string? description)
    {
        Identifier = NormalizeIdentifier(identifier);
        Name = name?.Trim() ?? string.Empty;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void MarkAsBorrowed()
    {
        IsAvailable = false;
    }

    public void MarkAsReturned()
    {
        IsAvailable = true;
    }
}
