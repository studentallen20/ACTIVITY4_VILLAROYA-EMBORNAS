using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // BR-05, BR-16: Required unique physical identifier (barcode/code), normalized uppercase
        builder.Property(e => e.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Identifier)
            .IsUnique()
            .HasDatabaseName("IX_Equipment_Identifier");

        // BR-06: Required nonblank name
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        // BR-06: Optional description
        builder.Property(e => e.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        // BR-06: Newly registered is available
        builder.Property(e => e.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(e => e.IsAvailable)
            .HasDatabaseName("IX_Equipment_IsAvailable");
    }
}
