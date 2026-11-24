using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Tests.Helpers;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class MultiItemCheckoutTests
{
    [Fact]
    public async Task UC06_MultiItemCheckout_Success_PersistsAllItemsAndMarksUnavailable()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC06_MultiItemCheckout_Success_PersistsAllItemsAndMarksUnavailable));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var student = new Student(1, "2024-0001", "Ana Reyes", null, isEligibleToBorrow: true, maxBorrowLimit: 3);
        var eq1 = new Equipment(1, "EQ-001", "Laptop 01", isAvailable: true);
        var eq2 = new Equipment(2, "EQ-002", "Projector 01", isAvailable: true);

        await context.Students.AddAsync(student);
        await context.Equipment.AddRangeAsync(eq1, eq2);
        await context.SaveChangesAsync();

        var service = new CheckOutMultipleEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await service.ExecuteAsync(student.Id, new[] { eq1.Id, eq2.Id }, dueDate);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Borrowings.Count);

        var updatedEq1 = await equipmentRepo.GetByIdAsync(eq1.Id);
        var updatedEq2 = await equipmentRepo.GetByIdAsync(eq2.Id);
        Assert.False(updatedEq1!.IsAvailable);
        Assert.False(updatedEq2!.IsAvailable);

        int activeLoans = await borrowingRepo.GetActiveBorrowingCountAsync(student.Id);
        Assert.Equal(2, activeLoans);
    }

    [Fact]
    public async Task UC06_MultiItemCheckout_FewerThanTwoItems_FailsValidation()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC06_MultiItemCheckout_FewerThanTwoItems_FailsValidation));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var service = new CheckOutMultipleEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);

        // Act (only 1 item provided)
        var result = await service.ExecuteAsync(1, new[] { 101 }, DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("requires at least two items"));
    }

    [Fact]
    public async Task UC06_MultiItemCheckout_DuplicateItemSelections_FailsValidation()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC06_MultiItemCheckout_DuplicateItemSelections_FailsValidation));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var service = new CheckOutMultipleEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);

        // Act (duplicate item 101 selected twice)
        var result = await service.ExecuteAsync(1, new[] { 101, 101 }, DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate equipment selections"));
    }

    [Fact]
    public async Task UC06_MultiItemCheckout_InvalidDueDate_FailsValidation()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC06_MultiItemCheckout_InvalidDueDate_FailsValidation));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var service = new CheckOutMultipleEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);

        // Act (due date is yesterday)
        var result = await service.ExecuteAsync(1, new[] { 101, 102 }, DateTime.UtcNow.AddDays(-1));

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("must be later than today"));
    }

    [Fact]
    public async Task UC06_MultiItemCheckout_ExceedsMaxBorrowLimit_FailsValidation()
    {
        // Arrange
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC06_MultiItemCheckout_ExceedsMaxBorrowLimit_FailsValidation));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        // Student has limit of 3, already has 2 active loans
        var student = new Student(1, "2024-0001", "Ana Reyes", null, isEligibleToBorrow: true, maxBorrowLimit: 3);
        var eqExisting1 = new Equipment(1, "EQ-001", "Camera 1", isAvailable: false);
        var eqExisting2 = new Equipment(2, "EQ-002", "Camera 2", isAvailable: false);

        var loan1 = new Borrowing(student.Id, eqExisting1.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5));
        var loan2 = new Borrowing(student.Id, eqExisting2.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5));

        var eqNew1 = new Equipment(3, "EQ-003", "Mic 1", isAvailable: true);
        var eqNew2 = new Equipment(4, "EQ-004", "Mic 2", isAvailable: true);

        await context.Students.AddAsync(student);
        await context.Equipment.AddRangeAsync(eqExisting1, eqExisting2, eqNew1, eqNew2);
        await context.Borrowings.AddRangeAsync(loan1, loan2);
        await context.SaveChangesAsync();

        var service = new CheckOutMultipleEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);

        // Act (requesting 2 more items, which would total 4 > 3 limit)
        var result = await service.ExecuteAsync(student.Id, new[] { eqNew1.Id, eqNew2.Id }, DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("exceeding the limit of 3"));
    }
}
