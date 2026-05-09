namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CustomerProfileService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<CustomerProfile, CustomerProfileDescriptor, CustomerProfileReadDto, CustomerProfileWriteDto, CustomerProfileDescriptorDto>
    (
        mediator,
        profile => profile.ToDto(),
        profile => profile.ToDomain(),
        profile => profile.ToDomain()
    ),
    ICustomerProfileService
{
    public async Task<CustomerProfileReadDto?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        CustomerProfile? profile = await dbContext.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return profile?.ToDto();
    }

    public async Task<CustomerProfileReadDto> EnsureProfileAsync(string userId, CancellationToken cancellationToken)
    {
        CustomerProfileReadDto? existing = await GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        CustomerProfile profile = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        _ = await dbContext.CustomerProfiles.AddAsync(profile, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }
}
