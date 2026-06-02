namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class RegisterCodeSystemHandler(MedicalDbContext dbContext)
    : IRequestHandler<RegisterCodeSystemCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(RegisterCodeSystemCommand request, CancellationToken cancellationToken)
    {
        if (!MedicalValidationRules.IsValidUri(request.CanonicalUri))
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Invalid canonical URI format."));
        }

        bool exists = await dbContext.CodeSystems
            .AnyAsync(cs => cs.SystemKey == request.SystemKey, cancellationToken);

        if (exists)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation($"Code system with key '{request.SystemKey}' already exists."));
        }

        CodeSystem entity = new()
        {
            Id = Guid.NewGuid(),
            SystemKey = request.SystemKey,
            Name = request.Name,
            Authority = request.Authority,
            Category = request.Category,
            CanonicalUri = request.CanonicalUri,
            Description = request.Description,
            IsActive = true
        };

        _ = await dbContext.CodeSystems.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return EntityResult<Guid>.Success(entity.Id);
    }
}
