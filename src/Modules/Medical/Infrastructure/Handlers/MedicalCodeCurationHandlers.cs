namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class CreateLocalMedicalCodeHandler(MedicalDbContext dbContext)
    : IRequestHandler<CreateLocalMedicalCodeCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateLocalMedicalCodeCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        bool releaseExists = await dbContext.CodeSystemReleases
            .AnyAsync(r => r.Id == request.CodeSystemReleaseId, cancellationToken);

        if (!releaseExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Code system release '{request.CodeSystemReleaseId}' not found."));
        }

        bool duplicateExists = await dbContext.MedicalCodes
            .AnyAsync(c => c.CodeSystemReleaseId == request.CodeSystemReleaseId
                           && c.Code == request.Code
                           && c.RecordOrigin == RecordOrigin.Local, cancellationToken);

        if (duplicateExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Local code '{request.Code}' already exists in this release."));
        }

        MedicalCode entity = new()
        {
            Id = Guid.NewGuid(),
            CodeSystemReleaseId = request.CodeSystemReleaseId,
            Code = request.Code,
            DisplayName = request.DisplayName,
            LongDescription = request.LongDescription,
            Status = MedicalCodeStatus.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            RecordOrigin = RecordOrigin.Local
        };

        _ = await dbContext.MedicalCodes.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class RetireMedicalCodeHandler(MedicalDbContext dbContext)
    : IRequestHandler<RetireMedicalCodeCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RetireMedicalCodeCommand request, CancellationToken cancellationToken)
    {
        MedicalCode? entity = await dbContext.MedicalCodes
            .FirstOrDefaultAsync(c => c.Id == request.MedicalCodeId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Medical code '{request.MedicalCodeId}' not found."));
        }

        if (MedicalValidationRules.IsImported(entity.RecordOrigin) && entity.Status == MedicalCodeStatus.Imported)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Cannot directly retire an imported code; use deprecation workflow."));
        }

        entity.Status = MedicalCodeStatus.Retired;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
