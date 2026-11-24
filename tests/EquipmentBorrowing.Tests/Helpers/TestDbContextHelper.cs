using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Tests.Helpers;

public class TestUnitOfWork : IUnitOfWork
{
    private readonly EquipmentBorrowingDbContext _context;

    public TestUnitOfWork(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        return Task.FromResult<ITransactionScope>(new TestTransactionScope());
    }

    private sealed class TestTransactionScope : ITransactionScope
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public static class TestDbContextHelper
{
    public static EquipmentBorrowingDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new EquipmentBorrowingDbContext(options);
    }
}
