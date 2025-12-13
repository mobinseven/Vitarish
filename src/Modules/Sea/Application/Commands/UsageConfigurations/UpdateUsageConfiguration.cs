using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Application.Commands.UsageConfigurations;

public record UpdateUsageConfigurationCommand(string ReminderSignature, string MetadataSchema) : IRequest<UsageConfiguration?>;

