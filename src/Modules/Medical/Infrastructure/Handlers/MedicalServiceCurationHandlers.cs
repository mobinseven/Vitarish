namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class CreateMedicalServiceHandler(MedicalDbContext dbContext)
    : IRequestHandler<CreateMedicalServiceCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateMedicalServiceCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        MedicalService entity = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Category = request.Category,
            Specialty = request.Specialty,
            Description = request.Description,
            Status = MedicalServiceStatus.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            RecordOrigin = RecordOrigin.Local
        };

        _ = await dbContext.MedicalServices.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class UpdateLocalMedicalServiceHandler(MedicalDbContext dbContext)
    : IRequestHandler<UpdateLocalMedicalServiceCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(UpdateLocalMedicalServiceCommand request, CancellationToken cancellationToken)
    {
        MedicalService? entity = await dbContext.MedicalServices
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Medical service '{request.ServiceId}' not found."));
        }

        if (MedicalValidationRules.IsImported(entity.RecordOrigin))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Cannot update an imported service. Create a local record instead."));
        }

        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        entity.Name = request.Name;
        entity.Category = request.Category;
        entity.Specialty = request.Specialty;
        entity.Description = request.Description;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}

internal sealed class RetireMedicalServiceHandler(MedicalDbContext dbContext)
    : IRequestHandler<RetireMedicalServiceCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RetireMedicalServiceCommand request, CancellationToken cancellationToken)
    {
        MedicalService? entity = await dbContext.MedicalServices
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (entity is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Medical service '{request.ServiceId}' not found."));
        }

        entity.Status = MedicalServiceStatus.Retired;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
