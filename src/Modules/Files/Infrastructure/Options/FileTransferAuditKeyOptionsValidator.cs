using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Vitarish.Files.Infrastructure.Options;

public sealed class FileTransferAuditKeyOptionsValidator(IHostEnvironment? environment) : IValidateOptions<FileTransferAuditKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, FileTransferAuditKeyOptions options) =>
        ResolveKey(options, environment) is null
            ? ValidateOptionsResult.Fail($"{FileTransferAuditKeyOptions.SectionName}:{nameof(FileTransferAuditKeyOptions.CurrentKey)} must be at least {FileTransferAuditKeyOptions.MinimumKeyLength} characters and supplied by protected configuration.")
            : ValidateOptionsResult.Success;

    internal static string? ResolveKey(FileTransferAuditKeyOptions options, IHostEnvironment? environment)
    {
        if (!string.IsNullOrWhiteSpace(options.CurrentKey))
            return options.CurrentKey.Length >= FileTransferAuditKeyOptions.MinimumKeyLength ? options.CurrentKey : null;
        return IsNonProductionComposition(environment) ? FileTransferAuditKeyOptions.NonProductionKey : null;
    }

    private static bool IsNonProductionComposition(IHostEnvironment? environment) =>
        environment is null ||
        environment.IsDevelopment() ||
        string.Equals(environment.EnvironmentName, "OpenApiGeneration", StringComparison.OrdinalIgnoreCase);
}
