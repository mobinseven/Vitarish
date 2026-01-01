using Vitarish.Sea.Application.Commands.UsageConfigurations;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class UpdateUsageConfigurationCommandHandler(IEntityRepository<UsageConfiguration, UsageConfigurationDescriptor> repository)
    : IRequestHandler<UpdateUsageConfigurationCommand, UsageConfiguration?>
{
    public async Task<UsageConfiguration?> Handle(UpdateUsageConfigurationCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<UsageConfiguration> all = await repository.GetAsync(new UsageConfigurationDescriptor(), cancellationToken) ?? [];
        UsageConfiguration? entity = all.FirstOrDefault();
        if (entity != null)
        {
            entity.ReminderSignature = request.ReminderSignature;
            entity.Metadata = request.MetadataSchema;
            return await repository.UpdateAsync(entity, cancellationToken);

        }
        else
        {
            entity = new()
            {
                ReminderSignature = request.ReminderSignature,
                Metadata = request.MetadataSchema
            };
            return await repository.AddAsync(entity, cancellationToken);

        }
    }
}

