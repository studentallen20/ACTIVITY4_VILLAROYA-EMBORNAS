using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfBorrowingRepository(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Borrowing?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Borrowings
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<int> GetActiveBorrowingCountAsync(int studentId, CancellationToken ct = default)
    {
        return await _context.Borrowings
            .CountAsync(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, ct);
    }

    public async Task<Borrowing?> GetActiveBorrowingByEquipmentIdAsync(int equipmentId, CancellationToken ct = default)
    {
        return await _context.Borrowings
            .FirstOrDefaultAsync(b => b.EquipmentId == equipmentId && b.Status == BorrowingStatus.Active, ct);
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> GetActiveBorrowingsAsync(CancellationToken ct = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.Status == BorrowingStatus.Active)
            .OrderBy(b => b.DueDate)
            .ThenBy(b => b.Id)
            .Select(b => ProjectToDto(b))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> GetHistoryByStudentIdAsync(int studentId, CancellationToken ct = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.BorrowDate)
            .ThenByDescending(b => b.Id)
            .Select(b => ProjectToDto(b))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> GetHistoryByEquipmentIdAsync(int equipmentId, CancellationToken ct = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .Where(b => b.EquipmentId == equipmentId)
            .OrderByDescending(b => b.BorrowDate)
            .ThenByDescending(b => b.Id)
            .Select(b => ProjectToDto(b))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BorrowingDetailDto>> SearchAndFilterAsync(
        string? searchText,
        BorrowingStatus? status,
        string? sortBy,
        bool ascending,
        CancellationToken ct = default)
    {
        var query = _context.Borrowings
            .AsNoTracking()
            .Include(b => b.Student)
            .Include(b => b.Equipment)
            .AsQueryable();

        // Status Filter
        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        // Partial Text Search (case-insensitive across student name, student number, and equipment name/identifier)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string term = searchText.Trim().ToLower();
            query = query.Where(b =>
                (b.Student != null && (b.Student.Name.ToLower().Contains(term) || b.Student.StudentNumber.ToLower().Contains(term))) ||
                (b.Equipment != null && (b.Equipment.Name.ToLower().Contains(term) || b.Equipment.Identifier.ToLower().Contains(term))));
        }

        // Sorting with stable tie-breaker ID
        bool isDueDateSort = string.Equals(sortBy, "DueDate", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(sortBy, "ExpectedReturnDate", StringComparison.OrdinalIgnoreCase);

        if (isDueDateSort)
        {
            query = ascending
                ? query.OrderBy(b => b.DueDate).ThenBy(b => b.Id)
                : query.OrderByDescending(b => b.DueDate).ThenByDescending(b => b.Id);
        }
        else
        {
            query = ascending
                ? query.OrderBy(b => b.BorrowDate).ThenBy(b => b.Id)
                : query.OrderByDescending(b => b.BorrowDate).ThenByDescending(b => b.Id);
        }

        return await query.Select(b => ProjectToDto(b)).ToListAsync(ct);
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken ct = default)
    {
        await _context.Borrowings.AddAsync(borrowing, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Borrowing> borrowings, CancellationToken ct = default)
    {
        await _context.Borrowings.AddRangeAsync(borrowings, ct);
    }

    public Task UpdateAsync(Borrowing borrowing, CancellationToken ct = default)
    {
        _context.Borrowings.Update(borrowing);
        return Task.CompletedTask;
    }

    private static BorrowingDetailDto ProjectToDto(Borrowing b)
    {
        return new BorrowingDetailDto
        {
            Id = b.Id,
            StudentId = b.StudentId,
            StudentNumber = b.Student != null ? b.Student.StudentNumber : string.Empty,
            StudentName = b.Student != null ? b.Student.Name : $"Student #{b.StudentId}",
            EquipmentId = b.EquipmentId,
            EquipmentIdentifier = b.Equipment != null ? b.Equipment.Identifier : string.Empty,
            EquipmentName = b.Equipment != null ? b.Equipment.Name : $"Equipment #{b.EquipmentId}",
            BorrowDate = b.BorrowDate,
            DueDate = b.DueDate,
            ReturnDate = b.ReturnDate,
            Status = b.Status
        };
    }
}
