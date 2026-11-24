using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        // BR-01, BR-16: Required institutional student number, normalized uppercase, unique constraint
        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.StudentNumber)
            .IsUnique()
            .HasDatabaseName("IX_Students_StudentNumber");

        // BR-02: Nonblank full name
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        // BR-02: Optional contact information
        builder.Property(s => s.ContactInfo)
            .IsRequired(false)
            .HasMaxLength(150);

        builder.Property(s => s.IsEligibleToBorrow)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.MaxBorrowLimit)
            .IsRequired()
            .HasDefaultValue(3);
    }
}
