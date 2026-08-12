using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.PatientAgent.Operations;

namespace Vitarish.Clinic.Application.PatientAgent;

[JsonSerializable(typeof(PatientAgentSearchResult))]
internal sealed partial class PatientAgentJsonSerializerContext : JsonSerializerContext;
