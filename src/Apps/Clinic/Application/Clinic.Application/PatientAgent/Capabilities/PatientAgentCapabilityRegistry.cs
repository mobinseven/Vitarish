using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent.Policies;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities;

public sealed class PatientAgentCapabilityRegistry
{
    private readonly IReadOnlyCollection<IPatientAgentCapability> _capabilities;
    private readonly PatientAgentAuthorizationPolicy _authorizationPolicy;
    private readonly PatientAgentToolAvailabilityPolicy _availabilityPolicy;

    public PatientAgentCapabilityRegistry(
        IEnumerable<IPatientAgentCapability> capabilities,
        PatientAgentAuthorizationPolicy authorizationPolicy,
        PatientAgentToolAvailabilityPolicy availabilityPolicy)
    {
        _authorizationPolicy = authorizationPolicy;
        _availabilityPolicy = availabilityPolicy;
        _capabilities = capabilities.ToArray();
        ValidateRegistrations(_capabilities);
    }

    public IReadOnlyCollection<IPatientAgentCapability> GetAvailable(PatientAgentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _capabilities
            .Where(capability => _availabilityPolicy.IsAvailable(capability, context))
            .Where(capability => _authorizationPolicy.IsAuthorized(capability, context))
            .ToArray();
    }

    private static void ValidateRegistrations(IReadOnlyCollection<IPatientAgentCapability> capabilities)
    {
        if (capabilities.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != capabilities.Count)
        {
            throw new InvalidOperationException("Patient agent capability names must be unique.");
        }

        var tools = capabilities.SelectMany(item => item.Tools).ToArray();
        if (tools.Select(tool => tool.Definition.Name).Distinct(StringComparer.Ordinal).Count() != tools.Length)
        {
            throw new InvalidOperationException("Patient agent tool names must be stable and unique.");
        }

        foreach (var tool in tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Definition.Name))
            {
                throw new InvalidOperationException("A patient agent tool name is required.");
            }

            using JsonDocument schema = JsonDocument.Parse(tool.Definition.ParametersJsonSchema);
            if (ContainsPatientIdProperty(schema.RootElement))
            {
                throw new InvalidOperationException("Patient agent tools must use the route-scoped patient and cannot accept patientId.");
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
