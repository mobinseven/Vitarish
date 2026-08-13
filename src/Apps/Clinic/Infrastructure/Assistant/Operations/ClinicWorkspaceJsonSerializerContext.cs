using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Assistant.Operations;

[JsonSerializable(typeof(ClinicWorkspaceCandidatePayload))]
internal sealed partial class ClinicWorkspaceJsonSerializerContext : JsonSerializerContext;
