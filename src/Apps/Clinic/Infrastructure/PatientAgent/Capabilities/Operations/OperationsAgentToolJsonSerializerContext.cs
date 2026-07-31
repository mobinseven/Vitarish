using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;

[JsonSerializable(typeof(OperationsPreparedActionResult))]
[JsonSerializable(typeof(OperationsActionReview))]
internal sealed partial class OperationsAgentToolJsonSerializerContext : JsonSerializerContext;
