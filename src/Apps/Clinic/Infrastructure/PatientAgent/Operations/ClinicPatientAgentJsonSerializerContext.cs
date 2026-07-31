using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Operations;

[JsonSerializable(typeof(PatientAgentCandidatePayload))]
internal sealed partial class ClinicPatientAgentJsonSerializerContext : JsonSerializerContext;
