using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class RegisterEquipmentService
{
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterEquipmentService(IEquipmentRepository equipmentRepo, IUnitOfWork unitOfWork)
    {
        _equipmentRepo = equipmentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<Equipment>> ExecuteAsync(
        string identifier,
        string name,
        string? description,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // BR-05: Nonblank unique equipment identifier required
        if (string.IsNullOrWhiteSpace(identifier))
        {
            errors.Add("Equipment identifier (barcode/code) is required.");
        }

        // BR-06: Nonblank name required
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Equipment name is required.");
        }

        if (errors.Any())
        {
            return OperationResult<Equipment>.Fail("Validation failed.", errors);
        }

        string normalizedId = Equipment.NormalizeIdentifier(identifier);

        // BR-05 & BR-16: Enforce uniqueness
        bool exists = await _equipmentRepo.ExistsByIdentifierAsync(normalizedId, null, ct);
        if (exists)
        {
            return OperationResult<Equipment>.Fail($"Equipment with identifier '{normalizedId}' already exists.");
        }

        // BR-06: Newly registered equipment is available with no active borrowing
        var equipment = new Equipment(normalizedId, name, description, isAvailable: true);

        await _equipmentRepo.AddAsync(equipment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<Equipment>.Ok(equipment, $"Equipment '{equipment.Name}' ({equipment.Identifier}) registered successfully and marked available.");
    }
}
