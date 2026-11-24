using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Tests.Helpers;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class StudentAndEquipmentManagementTests
{
    [Fact]
    public async Task UC12_RegisterStudent_UniqueNormalizedNumber_Succeeds()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC12_RegisterStudent_UniqueNormalizedNumber_Succeeds));
        var studentRepo = new EfStudentRepository(context);
        var uow = new TestUnitOfWork(context);
        var service = new RegisterStudentService(studentRepo, uow);

        var result = await service.ExecuteAsync("  2024-0010  ", "Crisostomo Ibarra", "09112223333");

        Assert.True(result.Success);
        Assert.Equal("2024-0010", result.Data!.StudentNumber);
        Assert.True(result.Data.IsEligibleToBorrow);
        Assert.Equal(3, result.Data.MaxBorrowLimit);
    }

    [Fact]
    public async Task UC12_RegisterStudent_DuplicateNumber_Rejected()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC12_RegisterStudent_DuplicateNumber_Rejected));
        var studentRepo = new EfStudentRepository(context);
        var uow = new TestUnitOfWork(context);

        await studentRepo.AddAsync(new Student("2024-0001", "Existing Student"));
        await uow.SaveChangesAsync();

        var service = new RegisterStudentService(studentRepo, uow);

        // Act (different case and whitespace)
        var result = await service.ExecuteAsync(" 2024-0001 ", "New Person", null);

        Assert.False(result.Success);
        Assert.Contains("already registered", result.Message);
    }

    [Fact]
    public async Task UC14_ChangeEligibility_RestrictedStudent_BlocksNewBorrowingButAllowsReturn()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC14_ChangeEligibility_RestrictedStudent_BlocksNewBorrowingButAllowsReturn));
        var studentRepo = new EfStudentRepository(context);
        var equipmentRepo = new EfEquipmentRepository(context);
        var borrowingRepo = new EfBorrowingRepository(context);
        var uow = new TestUnitOfWork(context);

        var student = new Student(1, "2024-0001", "Pedro Penduko", null, isEligibleToBorrow: true);
        var equipment1 = new Equipment(1, "EQ-001", "Active Loan Item", isAvailable: false);
        var equipment2 = new Equipment(2, "EQ-002", "New Request Item", isAvailable: true);
        var activeLoan = new Borrowing(student.Id, equipment1.Id, System.DateTime.UtcNow.AddDays(-2), System.DateTime.UtcNow.AddDays(5));

        await context.Students.AddAsync(student);
        await context.Equipment.AddRangeAsync(equipment1, equipment2);
        await context.Borrowings.AddAsync(activeLoan);
        await context.SaveChangesAsync();

        var eligibilityService = new ChangeStudentEligibilityService(studentRepo, uow);
        var singleBorrowService = new BorrowEquipmentService(studentRepo, equipmentRepo, borrowingRepo, uow);
        var returnService = new ReturnEquipmentService(borrowingRepo, equipmentRepo, uow);

        // 1. Restrict student (UC-14)
        var restrictResult = await eligibilityService.ExecuteAsync(student.Id, makeEligible: false);
        Assert.True(restrictResult.Success);
        Assert.False(restrictResult.Data!.IsEligibleToBorrow);

        // 2. Attempt new borrow while restricted -> Must fail (BR-04, BR-08)
        var borrowResult = await singleBorrowService.ExecuteAsync(student.Id, equipment2.Id, System.DateTime.UtcNow.AddDays(7));
        Assert.False(borrowResult.Success);
        Assert.Contains("restricted from borrowing", borrowResult.Message);

        // 3. Return existing active loan while restricted -> Must succeed (BR-04)
        var returnResult = await returnService.ExecuteAsync(activeLoan.Id);
        Assert.True(returnResult.Success);
        Assert.Equal(BorrowingStatus.Returned, returnResult.Borrowing!.Status);
    }

    [Fact]
    public async Task UC15_RegisterEquipment_UniqueNormalizedIdentifier_SucceedsAndAvailableByDefault()
    {
        using var context = TestDbContextHelper.CreateInMemoryContext(nameof(UC15_RegisterEquipment_UniqueNormalizedIdentifier_SucceedsAndAvailableByDefault));
        var equipmentRepo = new EfEquipmentRepository(context);
        var uow = new TestUnitOfWork(context);
        var service = new RegisterEquipmentService(equipmentRepo, uow);

        var result = await service.ExecuteAsync("  eq-999  ", "VR Headset", "Oculus Quest 3");

        Assert.True(result.Success);
        Assert.Equal("EQ-999", result.Data!.Identifier);
        Assert.True(result.Data.IsAvailable);
    }
}
