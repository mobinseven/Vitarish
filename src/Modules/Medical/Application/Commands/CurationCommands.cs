using Vitarish.Entities.Contracts;

namespace Vitarish.Medical.Application.Commands;

public record CreateLocalMedicalCodeCommand(
    Guid CodeSystemReleaseId,
    string Code,
    string DisplayName,
    string? LongDescription,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<EntityResult<Guid>>;

public record RetireMedicalCodeCommand(Guid MedicalCodeId) : IRequest<EntityResult<Guid>>;

public record CreateCodeMapCommand(
    Guid SourceMedicalCodeId,
    Guid TargetMedicalCodeId,
    MappingType MappingType,
    decimal Confidence,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? SourceNote) : IRequest<EntityResult<Guid>>;

public record RetireCodeMapCommand(Guid CodeMapId) : IRequest<EntityResult<Guid>>;

public record CreateDrugCommand(
    string PrimaryName,
    DrugIdentifierType PrimaryIdentifierType,
    string PrimaryIdentifierValue,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<EntityResult<Guid>>;

public record UpdateLocalDrugCommand(
    Guid DrugId,
    string PrimaryName,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    DrugIdentifierType? PrimaryIdentifierType = null,
    string? PrimaryIdentifierValue = null) : IRequest<EntityResult<Guid>>;

public record RetireDrugCommand(Guid DrugId) : IRequest<EntityResult<Guid>>;

public record CreateMedicalServiceCommand(
    string Name,
    string? Category,
    string? Specialty,
    string? Description,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<EntityResult<Guid>>;

public record UpdateLocalMedicalServiceCommand(
    Guid ServiceId,
    string Name,
    string? Category,
    string? Specialty,
    string? Description,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<EntityResult<Guid>>;

public record RetireMedicalServiceCommand(Guid ServiceId) : IRequest<EntityResult<Guid>>;
