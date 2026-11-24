using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Equipment?> GetByIdentifierAsync(string identifier, CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken ct = default);
    Task AddAsync(Equipment equipment, CancellationToken ct = default);
    Task UpdateAsync(Equipment equipment, CancellationToken ct = default);
    Task<bool> ExistsByIdentifierAsync(string identifier, int? excludeId = null, CancellationToken ct = default);
}
