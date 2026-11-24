using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfUnitOfWork(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        var tx = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(tx);
    }

    private sealed class EfTransactionScope : ITransactionScope
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransactionScope(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await _transaction.CommitAsync(ct);
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            await _transaction.RollbackAsync(ct);
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
        }
    }
}
