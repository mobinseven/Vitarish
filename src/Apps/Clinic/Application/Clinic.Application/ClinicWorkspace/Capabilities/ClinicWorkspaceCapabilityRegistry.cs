using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public sealed class ClinicWorkspaceCapabilityRegistry
{
    private readonly IReadOnlyCollection<IClinicWorkspaceCapability> _capabilities;
    private readonly ClinicWorkspaceAuthorizationPolicy _authorizationPolicy;
    private readonly ClinicWorkspaceToolAvailabilityPolicy _availabilityPolicy;

    public ClinicWorkspaceCapabilityRegistry(
        IEnumerable<IClinicWorkspaceCapability> capabilities,
        ClinicWorkspaceAuthorizationPolicy authorizationPolicy,
        ClinicWorkspaceToolAvailabilityPolicy availabilityPolicy)
    {
        _authorizationPolicy = authorizationPolicy;
        _availabilityPolicy = availabilityPolicy;
        _capabilities = capabilities.ToArray();
        ValidateRegistrations(_capabilities);
    }

    public IReadOnlyCollection<IClinicWorkspaceCapability> GetAvailable(ClinicWorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _capabilities
            .Where(capability => _availabilityPolicy.IsAvailable(capability, context))
            .Where(capability => _authorizationPolicy.IsAuthorized(capability, context))
            .ToArray();
    }

    private static void ValidateRegistrations(IReadOnlyCollection<IClinicWorkspaceCapability> capabilities)
    {
        if (capabilities.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != capabilities.Count)
        {
            throw new InvalidOperationException("Clinic assistant capability names must be unique.");
        }

        var tools = capabilities.SelectMany(item => item.Tools).ToArray();
        if (tools.Select(tool => tool.Definition.Name).Distinct(StringComparer.Ordinal).Count() != tools.Length)
        {
            throw new InvalidOperationException("Clinic assistant tool names must be stable and unique.");
        }

        foreach (var tool in tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Definition.Name))
            {
                throw new InvalidOperationException("A clinic assistant tool name is required.");
            }

            using JsonDocument schema = JsonDocument.Parse(tool.Definition.ParametersJsonSchema);
            if (ContainsPatientIdProperty(schema.RootElement))
            {
                throw new InvalidOperationException("Clinic assistant tools must use the route-scoped patient and cannot accept patientId.");
            }
        }
    }

    private static bool ContainsPatientIdProperty(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Any(property =>
            string.Equals(property.Name, "patientId", StringComparison.OrdinalIgnoreCase) || ContainsPatientIdProperty(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Any(ContainsPatientIdProperty),
        _ => false
    };
}
