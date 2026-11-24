using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _context;

    public EfStudentRepository(EquipmentBorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken ct = default)
    {
        string normalized = Student.NormalizeStudentNumber(studentNumber);
        return await _context.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == normalized, ct);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Students
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Student student, CancellationToken ct = default)
    {
        await _context.Students.AddAsync(student, ct);
    }

    public Task UpdateAsync(Student student, CancellationToken ct = default)
    {
        _context.Students.Update(student);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByStudentNumberAsync(string studentNumber, int? excludeId = null, CancellationToken ct = default)
    {
        string normalized = Student.NormalizeStudentNumber(studentNumber);
        var query = _context.Students.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(s => s.StudentNumber == normalized, ct);
    }
}
