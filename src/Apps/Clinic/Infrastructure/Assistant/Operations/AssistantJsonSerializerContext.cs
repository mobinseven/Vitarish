using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Assistant.Operations;

[JsonSerializable(typeof(AssistantCandidatePayload))]
internal sealed partial class AssistantJsonSerializerContext : JsonSerializerContext;
