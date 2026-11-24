using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.StudentId)
            .IsRequired();

        builder.Property(b => b.EquipmentId)
            .IsRequired();

        builder.Property(b => b.BorrowDate)
            .IsRequired();

        builder.Property(b => b.DueDate)
            .IsRequired();

        builder.Property(b => b.ReturnDate)
            .IsRequired(false);

        // Status stored as text enum ('Active', 'Returned')
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // BR-14: Foreign key relationships with restrict delete to prevent hard deletion
        builder.HasOne(b => b.Student)
            .WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Equipment)
            .WithMany()
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fast retrieval indexes
        builder.HasIndex(b => b.StudentId)
            .HasDatabaseName("IX_Borrowings_StudentId");

        builder.HasIndex(b => b.EquipmentId)
            .HasDatabaseName("IX_Borrowings_EquipmentId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Borrowings_Status");

        builder.HasIndex(b => b.BorrowDate)
            .HasDatabaseName("IX_Borrowings_BorrowDate");

        builder.HasIndex(b => b.DueDate)
            .HasDatabaseName("IX_Borrowings_DueDate");

        // BR-10 & BR-16: Database-enforced single-active-borrowing protection.
        // A partial unique index in PostgreSQL ensures an equipment cannot have more than one Active loan at any time.
        builder.HasIndex(b => b.EquipmentId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'")
            .HasDatabaseName("IX_Borrowings_SingleActiveBorrowingPerEquipment");
    }
}
