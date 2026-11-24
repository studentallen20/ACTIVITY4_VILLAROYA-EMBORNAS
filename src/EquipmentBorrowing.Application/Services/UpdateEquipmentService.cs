using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class UpdateEquipmentService
{
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEquipmentService(IEquipmentRepository equipmentRepo, IUnitOfWork unitOfWork)
    {
        _equipmentRepo = equipmentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<Equipment>> ExecuteAsync(
        int equipmentId,
        string identifier,
        string name,
        string? description,
        CancellationToken ct = default)
    {
        var equipment = await _equipmentRepo.GetByIdAsync(equipmentId, ct);
        if (equipment == null)
        {
            return OperationResult<Equipment>.Fail($"Equipment with ID {equipmentId} was not found.");
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(identifier))
        {
            errors.Add("Equipment identifier cannot be blank.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Equipment name cannot be blank.");
        }

        if (errors.Any())
        {
            return OperationResult<Equipment>.Fail("Validation failed.", errors);
        }

        string normalizedId = Equipment.NormalizeIdentifier(identifier);

        // BR-05, BR-07: Uniqueness check if identifier changed
        bool duplicate = await _equipmentRepo.ExistsByIdentifierAsync(normalizedId, equipmentId, ct);
        if (duplicate)
        {
            return OperationResult<Equipment>.Fail($"Equipment identifier '{normalizedId}' is already in use by another item.");
        }

        // BR-07: Descriptive updates only. Availability and borrowing history remain intact.
        equipment.UpdateDescriptiveInfo(normalizedId, name, description);

        await _equipmentRepo.UpdateAsync(equipment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<Equipment>.Ok(equipment, $"Equipment #{equipmentId} ({equipment.Identifier}) updated successfully.");
    }
}
