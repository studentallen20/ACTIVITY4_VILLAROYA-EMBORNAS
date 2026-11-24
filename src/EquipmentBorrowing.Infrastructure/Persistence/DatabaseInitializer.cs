using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(EquipmentBorrowingDbContext context, CancellationToken ct = default)
    {
        // 1. Ensure migrations are applied on PostgreSQL
        await context.Database.MigrateAsync(ct);

        // 2. Seed initial Students if empty (BR-01, BR-02, BR-16)
        if (!await context.Students.AnyAsync(ct))
        {
            var students = new List<Student>
            {
                new("2024-0001", "Mharc Allen Villaroya", "09123456789", isEligibleToBorrow: true, maxBorrowLimit: 3),
                new("2024-0002", "Prince Lawrence Embornas", "09987654321", isEligibleToBorrow: false, maxBorrowLimit: 2),
                new("2024-0003", "Aliya Raymundo", null, isEligibleToBorrow: true, maxBorrowLimit: 1),
                new("2024-0004", "Juan Dela Cruz", "juan@campus.edu", isEligibleToBorrow: true, maxBorrowLimit: 3),
                new("2024-0005", "Maria Santos", "09187654321", isEligibleToBorrow: true, maxBorrowLimit: 3)
            };
            await context.Students.AddRangeAsync(students, ct);
            await context.SaveChangesAsync(ct);
        }

        // 3. Seed initial Equipment if empty (BR-05, BR-06, BR-16)
        if (!await context.Equipment.AnyAsync(ct))
        {
            var equipment = new List<Equipment>
            {
                new("EQ-001", "Epson LCD Projector", "HDMI/VGA compatible 3500 lumens", isAvailable: true),
                new("EQ-002", "Dell Latitude 5420 Laptop", "Core i5 16GB RAM 512GB SSD", isAvailable: false),
                new("EQ-003", "HDMI to USB-C Adapter", "Multi-port dongle with power passthrough", isAvailable: true),
                new("EQ-004", "Canon EOS 3000D DSLR Camera", "18-55mm lens with neck strap", isAvailable: true),
                new("EQ-005", "Wireless UHF Microphone Set", "Dual channel handheld mics with receiver", isAvailable: true),
                new("EQ-006", "Sony Alpha A6400 Mirrorless", "16-50mm kit lens with battery", isAvailable: false),
                new("EQ-007", "Logitech Presentation Clicker", "Wireless pointer with USB receiver", isAvailable: true)
            };
            await context.Equipment.AddRangeAsync(equipment, ct);
            await context.SaveChangesAsync(ct);
        }

        // 4. Seed initial Borrowings if empty (both Active and Returned for history testing)
        if (!await context.Borrowings.AnyAsync(ct))
        {
            var student1 = await context.Students.FirstAsync(s => s.StudentNumber == "2024-0001", ct);
            var student4 = await context.Students.FirstAsync(s => s.StudentNumber == "2024-0004", ct);
            var eq2 = await context.Equipment.FirstAsync(e => e.Identifier == "EQ-002", ct);
            var eq3 = await context.Equipment.FirstAsync(e => e.Identifier == "EQ-003", ct);
            var eq6 = await context.Equipment.FirstAsync(e => e.Identifier == "EQ-006", ct);

            var initialBorrowings = new List<Borrowing>
            {
                // Active loan 1: Student 1 borrowed Laptop
                new(student1.Id, eq2.Id, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(5))
                {
                    Status = BorrowingStatus.Active
                },
                // Active loan 2: Student 4 borrowed Sony Camera
                new(student4.Id, eq6.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6))
                {
                    Status = BorrowingStatus.Active
                },
                // Returned loan: Student 1 borrowed HDMI adapter and returned it
                new(student1.Id, eq3.Id, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3))
                {
                    Status = BorrowingStatus.Returned,
                    ReturnDate = DateTime.UtcNow.AddDays(-4)
                }
            };

            await context.Borrowings.AddRangeAsync(initialBorrowings, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
