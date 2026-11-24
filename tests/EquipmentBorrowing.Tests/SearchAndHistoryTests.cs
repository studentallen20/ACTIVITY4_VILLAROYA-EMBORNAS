using System;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Tests.Helpers;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class SearchAndHistoryTests
{
    [Fact]
    public async Task UC07_StudentHistory_ReturnsOnlySelectedStudentActiveAndReturnedLoans()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC07_StudentHistory_ReturnsOnlySelectedStudentActiveAndReturnedLoans));
        var studentRepo = new EfStudentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);

        var s1 = new Student(1, "2024-0001", "Student One");
        var s2 = new Student(2, "2024-0002", "Student Two");
        var eq1 = new Equipment(1, "EQ-001", "Laptop");
        var eq2 = new Equipment(2, "EQ-002", "Projector");

        var loan1 = new Borrowing(s1.Id, eq1.Id, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(2)) { Status = BorrowingStatus.Active };
        var loan2 = new Borrowing(s1.Id, eq2.Id, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-13)) { Status = BorrowingStatus.Returned, ReturnDate = DateTime.UtcNow.AddDays(-14) };
        var loan3 = new Borrowing(s2.Id, eq1.Id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-23)) { Status = BorrowingStatus.Returned, ReturnDate = DateTime.UtcNow.AddDays(-24) };

        await context.Students.AddRangeAsync(s1, s2);
        await context.Equipment.AddRangeAsync(eq1, eq2);
        await context.Borrowings.AddRangeAsync(loan1, loan2, loan3);
        await context.SaveChangesAsync();

        var service = new GetStudentBorrowingHistoryService(borrowingRepo, studentRepo);

        // Act
        var result = await service.ExecuteAsync(s1.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data, item => Assert.Equal("Student One", item.StudentName));
    }

    [Fact]
    public async Task UC08_EquipmentHistory_ReturnsOnlySelectedEquipmentLoans()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC08_EquipmentHistory_ReturnsOnlySelectedEquipmentLoans));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);

        var s1 = new Student(1, "2024-0001", "Student One");
        var eq1 = new Equipment(1, "EQ-001", "Laptop");
        var eq2 = new Equipment(2, "EQ-002", "Projector");

        var loan1 = new Borrowing(s1.Id, eq1.Id, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(2)) { Status = BorrowingStatus.Active };
        var loan2 = new Borrowing(s1.Id, eq2.Id, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3)) { Status = BorrowingStatus.Returned };

        await context.Students.AddAsync(s1);
        await context.Equipment.AddRangeAsync(eq1, eq2);
        await context.Borrowings.AddRangeAsync(loan1, loan2);
        await context.SaveChangesAsync();

        var service = new GetEquipmentBorrowingHistoryService(borrowingRepo, equipmentRepo);

        // Act
        var result = await service.ExecuteAsync(eq1.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("EQ-001", result.Data[0].EquipmentIdentifier);
    }

    [Fact]
    public async Task UC09_SearchBorrowings_FilteredAndSorted_ReturnsAccurateRecords()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC09_SearchBorrowings_FilteredAndSorted_ReturnsAccurateRecords));
        var borrowingRepo = new EfBorrowingRepository(context);

        var s1 = new Student(1, "2024-0001", "Alice Reyes");
        var s2 = new Student(2, "2024-0002", "Bob Santos");
        var eq1 = new Equipment(1, "EQ-001", "Dell XPS Laptop");
        var eq2 = new Equipment(2, "EQ-002", "Epson Projector");

        var loan1 = new Borrowing(s1.Id, eq1.Id, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(5)) { Status = BorrowingStatus.Active };
        var loan2 = new Borrowing(s2.Id, eq2.Id, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3)) { Status = BorrowingStatus.Returned, ReturnDate = DateTime.UtcNow.AddDays(-4) };

        await context.Students.AddRangeAsync(s1, s2);
        await context.Equipment.AddRangeAsync(eq1, eq2);
        await context.Borrowings.AddRangeAsync(loan1, loan2);
        await context.SaveChangesAsync();

        var service = new SearchBorrowingsService(borrowingRepo);

        // Act 1: Search by partial equipment name "Dell"
        var searchDell = await service.ExecuteAsync("dell", null);
        Assert.Single(searchDell);
        Assert.Equal("Dell XPS Laptop", searchDell[0].EquipmentName);

        // Act 2: Filter by Active status only
        var searchActive = await service.ExecuteAsync(null, BorrowingStatus.Active);
        Assert.Single(searchActive);
        Assert.Equal(BorrowingStatus.Active, searchActive[0].Status);

        // Act 3: Filter by Returned status only
        var searchReturned = await service.ExecuteAsync(null, BorrowingStatus.Returned);
        Assert.Single(searchReturned);
        Assert.Equal(BorrowingStatus.Returned, searchReturned[0].Status);
    }
}
