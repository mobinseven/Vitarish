namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class CreateDrugHandler(MedicalDbContext dbContext)
    : IRequestHandler<CreateDrugCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateDrugCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        bool hasOverlappingInterval = await dbContext.Drugs
            .AnyAsync(
                d => d.PrimaryIdentifierType == request.PrimaryIdentifierType
                    && d.PrimaryIdentifierValue == request.PrimaryIdentifierValue
                    && d.RecordOrigin == RecordOrigin.Local
                    && (
                        request.EffectiveTo == null || d.EffectiveFrom == null || d.EffectiveFrom <= request.EffectiveTo
                    )
                    && (
                        d.EffectiveTo == null || request.EffectiveFrom == null || request.EffectiveFrom <= d.EffectiveTo
                    ),
                cancellationToken);

        if (hasOverlappingInterval)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Effective date range overlaps an existing drug with the same identifier."));
        }

        Drug entity = new()
        {
            Id = Guid.NewGuid(),
            PrimaryName = request.PrimaryName,
            NormalizedName = request.PrimaryName.ToUpperInvariant(),
            PrimaryIdentifierType = request.PrimaryIdentifierType,
            PrimaryIdentifierValue = request.PrimaryIdentifierValue,
            Status = DrugStatus.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            RecordOrigin = RecordOrigin.Local
        };

        _ = await dbContext.Drugs.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class UpdateLocalDrugHandler(MedicalDbContext dbContext)
    : IRequestHandler<UpdateLocalDrugCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(UpdateLocalDrugCommand request, CancellationToken cancellationToken)
    {
        Drug? entity = await dbContext.Drugs
            .FirstOrDefaultAsync(d => d.Id == request.DrugId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Drug '{request.DrugId}' not found."));
        }

        if (MedicalValidationRules.IsImported(entity.RecordOrigin))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Cannot update an imported drug. Create a local record instead."));
        }

        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        bool hasOverlappingInterval = await dbContext.Drugs
            .AnyAsync(
                d => d.Id != request.DrugId
                    && d.PrimaryIdentifierType == entity.PrimaryIdentifierType
                    && d.PrimaryIdentifierValue == entity.PrimaryIdentifierValue
                    && d.RecordOrigin == RecordOrigin.Local
                    && (
                        request.EffectiveTo == null || d.EffectiveFrom == null || d.EffectiveFrom <= request.EffectiveTo
                    )
                    && (
                        d.EffectiveTo == null || request.EffectiveFrom == null || request.EffectiveFrom <= d.EffectiveTo
                    ),
                cancellationToken);

        if (hasOverlappingInterval)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Effective date range overlaps an existing drug with the same identifier."));
        }

        entity.PrimaryName = request.PrimaryName;
        entity.NormalizedName = request.PrimaryName.ToUpperInvariant();
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class RetireDrugHandler(MedicalDbContext dbContext)
    : IRequestHandler<RetireDrugCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RetireDrugCommand request, CancellationToken cancellationToken)
    {
        Drug? entity = await dbContext.Drugs
            .FirstOrDefaultAsync(d => d.Id == request.DrugId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Drug '{request.DrugId}' not found."));
        }

        entity.Status = DrugStatus.Retired;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
