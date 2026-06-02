namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class CreateCodeMapHandler(MedicalDbContext dbContext)
    : IRequestHandler<CreateCodeMapCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateCodeMapCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidConfidence(request.Confidence))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Confidence must be between 0 and 1."));
        }

        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        bool sourceExists = await dbContext.MedicalCodes
            .AnyAsync(c => c.Id == request.SourceMedicalCodeId, cancellationToken);

        if (!sourceExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Source medical code not found."));
        }

        bool targetExists = await dbContext.MedicalCodes
            .AnyAsync(c => c.Id == request.TargetMedicalCodeId, cancellationToken);

        if (!targetExists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Target medical code not found."));
        }


        bool hasOverlappingInterval = await dbContext.CodeMaps
            .AnyAsync(
                c => c.SourceMedicalCodeId == request.SourceMedicalCodeId
                    && c.TargetMedicalCodeId == request.TargetMedicalCodeId
                    && c.MappingType == request.MappingType
                    && c.RecordOrigin == RecordOrigin.Local
                    && (
                        request.EffectiveTo == null || c.EffectiveFrom == null || c.EffectiveFrom <= request.EffectiveTo
                    )
                    && (
                        c.EffectiveTo == null || request.EffectiveFrom == null || request.EffectiveFrom <= c.EffectiveTo
                    ),
                cancellationToken);

        if (hasOverlappingInterval)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Effective date range overlaps an existing code map for the same source, target, and mapping type."));
        }

        CodeMap entity = new()
        {
            Id = Guid.NewGuid(),
            SourceMedicalCodeId = request.SourceMedicalCodeId,
            TargetMedicalCodeId = request.TargetMedicalCodeId,
            MappingType = request.MappingType,
            Confidence = request.Confidence,
            Status = CodeMapStatus.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            RecordOrigin = RecordOrigin.Local,
            SourceNote = request.SourceNote
        };

        _ = await dbContext.CodeMaps.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class RetireCodeMapHandler(MedicalDbContext dbContext)
    : IRequestHandler<RetireCodeMapCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RetireCodeMapCommand request, CancellationToken cancellationToken)
    {
        CodeMap? entity = await dbContext.CodeMaps
            .FirstOrDefaultAsync(c => c.Id == request.CodeMapId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Code map '{request.CodeMapId}' not found."));
        }

        entity.Status = CodeMapStatus.Retired;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
