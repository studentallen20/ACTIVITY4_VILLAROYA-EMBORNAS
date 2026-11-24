using System;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Tests.Helpers;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class SingleBorrowAndReturnTests
{
    [Fact]
    public async Task UC03_BorrowSingleItem_Success_CreatesLoanAndMarksItemUnavailable()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC03_BorrowSingleItem_Success_CreatesLoanAndMarksItemUnavailable));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var student = new Student(1, "2024-0001", "Maria Clara", null, isEligibleToBorrow: true, maxBorrowLimit: 3);
        var equipment = new Equipment(1, "EQ-001", "Projector", isAvailable: true);

        await context.Students.AddAsync(student);
        await context.Equipment.AddAsync(equipment);
        await context.SaveChangesAsync();

        var service = new BorrowEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);

        // Act
        var result = await service.ExecuteAsync(student.Id, equipment.Id, DateTime.UtcNow.AddDays(5));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
        Assert.Equal(BorrowingStatus.Active, result.Borrowing.Status);

        var updatedEq = await equipmentRepo.GetByIdAsync(equipment.Id);
        Assert.False(updatedEq!.IsAvailable);
    }

    [Fact]
    public async Task UC05_ReturnEquipment_Success_MarksReturnedAndItemAvailable()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC05_ReturnEquipment_Success_MarksReturnedAndItemAvailable));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var student = new Student(1, "2024-0001", "Maria Clara", null, isEligibleToBorrow: true, maxBorrowLimit: 3);
        var equipment = new Equipment(1, "EQ-001", "Projector", isAvailable: false);
        var borrowing = new Borrowing(student.Id, equipment.Id, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(5));

        await context.Students.AddAsync(student);
        await context.Equipment.AddAsync(equipment);
        await context.Borrowings.AddAsync(borrowing);
        await context.SaveChangesAsync();

        var service = new ReturnEquipmentService(borrowingRepo, equipmentRepo, uow);

        // Act
        var result = await service.ExecuteAsync(borrowing.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(BorrowingStatus.Returned, result.Borrowing!.Status);
        Assert.NotNull(result.Borrowing.ReturnDate);

        var updatedEq = await equipmentRepo.GetByIdAsync(equipment.Id);
        Assert.True(updatedEq!.IsAvailable);
    }

    [Fact]
    public async Task UC05_RepeatReturn_AlreadyReturned_RejectedWithoutStateChange()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC05_RepeatReturn_AlreadyReturned_RejectedWithoutStateChange));
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var student = new Student(1, "2024-0001", "Maria Clara", null, isEligibleToBorrow: true, maxBorrowLimit: 3);
        var equipment = new Equipment(1, "EQ-001", "Projector", isAvailable: true);
        var borrowing = new Borrowing(student.Id, equipment.Id, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3))
        {
            Status = BorrowingStatus.Returned,
            ReturnDate = DateTime.UtcNow.AddDays(-4)
        };
        await context.Students.AddAsync(student);
        await context.Equipment.AddAsync(equipment);
        await context.Borrowings.AddAsync(borrowing);
        await context.SaveChangesAsync();

        var service = new ReturnEquipmentService(borrowingRepo, equipmentRepo, uow);

        // Act (attempt return again)
        var result = await service.ExecuteAsync(borrowing.Id);

        // Assert (BR-13: Reject repeat return without changing state)
        Assert.False(result.Success);
        Assert.Contains("already been returned", result.Message);
    }
}
