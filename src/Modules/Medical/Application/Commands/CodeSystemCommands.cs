using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Medical.Application.Commands;

public record RegisterCodeSystemCommand(
    string SystemKey,
    string Name,
    string? Authority,
    CodeSystemCategory Category,
    string? CanonicalUri,
    string? Description) : IRequest<EntityResult<Guid>>;

public record RegisterCodeSystemReleaseCommand(
    Guid CodeSystemId,
    string ReleaseName,
    DateOnly? ReleaseDate,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? SourceLocator,
    string? SourceVersion) : IRequest<EntityResult<Guid>>;

public record ActivateCodeSystemReleaseCommand(Guid ReleaseId) : IRequest<EntityResult<Guid>>;

public record DeactivateCodeSystemReleaseCommand(Guid ReleaseId) : IRequest<EntityResult<Guid>>;
