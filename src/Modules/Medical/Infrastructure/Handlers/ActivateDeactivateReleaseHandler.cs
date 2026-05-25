namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ActivateCodeSystemReleaseHandler(MedicalDbContext dbContext)
    : IRequestHandler<ActivateCodeSystemReleaseCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(ActivateCodeSystemReleaseCommand request, CancellationToken cancellationToken)
    {
        CodeSystemRelease? release = await dbContext.CodeSystemReleases
            .FirstOrDefaultAsync(r => r.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Release '{request.ReleaseId}' not found."));
        }

        release.IsActive = true;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(release.Id);
    }
}

internal sealed class DeactivateCodeSystemReleaseHandler(MedicalDbContext dbContext)
    : IRequestHandler<DeactivateCodeSystemReleaseCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(DeactivateCodeSystemReleaseCommand request, CancellationToken cancellationToken)
    {
        CodeSystemRelease? release = await dbContext.CodeSystemReleases
            .FirstOrDefaultAsync(r => r.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Release '{request.ReleaseId}' not found."));
        }

        release.IsActive = false;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(release.Id);
    }
}
