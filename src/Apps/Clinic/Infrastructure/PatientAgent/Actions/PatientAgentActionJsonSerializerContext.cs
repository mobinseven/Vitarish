using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.PatientAgent;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

[JsonSerializable(typeof(PatientAgentAction))]
[JsonSerializable(typeof(PatientAgentActionFailure))]
internal sealed partial class PatientAgentActionJsonSerializerContext : JsonSerializerContext;
