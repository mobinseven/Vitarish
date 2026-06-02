namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class RegisterCodeSystemReleaseHandler(MedicalDbContext dbContext)
    : IRequestHandler<RegisterCodeSystemReleaseCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RegisterCodeSystemReleaseCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        bool codeSystemExists = await dbContext.CodeSystems
            .AnyAsync(cs => cs.Id == request.CodeSystemId, cancellationToken);

        if (!codeSystemExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Code system '{request.CodeSystemId}' not found."));
        }

        bool releaseExists = await dbContext.CodeSystemReleases
            .AnyAsync(r => r.CodeSystemId == request.CodeSystemId && r.ReleaseName == request.ReleaseName, cancellationToken);

        if (releaseExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Release '{request.ReleaseName}' already exists for this code system."));
        }

        CodeSystemRelease entity = new()
        {
            Id = Guid.NewGuid(),
            CodeSystemId = request.CodeSystemId,
            ReleaseName = request.ReleaseName,
            ReleaseDate = request.ReleaseDate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            SourceLocator = request.SourceLocator,
            SourceVersion = request.SourceVersion,
            IsActive = false
        };

        _ = await dbContext.CodeSystemReleases.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
