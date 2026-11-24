using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfEquipmentRepository(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Equipment?> GetByIdentifierAsync(string identifier, CancellationToken ct = default)
    {
        string normalized = Equipment.NormalizeIdentifier(identifier);
        return await _context.Equipment
            .FirstOrDefaultAsync(e => e.Identifier == normalized, ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Equipment
            .AsNoTracking()
            .OrderBy(e => e.Identifier)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken ct = default)
    {
        return await _context.Equipment
            .AsNoTracking()
            .Where(e => e.IsAvailable)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken ct = default)
    {
        await _context.Equipment.AddAsync(equipment, ct);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken ct = default)
    {
        _context.Equipment.Update(equipment);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByIdentifierAsync(string identifier, int? excludeId = null, CancellationToken ct = default)
    {
        string normalized = Equipment.NormalizeIdentifier(identifier);
        var query = _context.Equipment.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync(e => e.Identifier == normalized, ct);
    }
}
