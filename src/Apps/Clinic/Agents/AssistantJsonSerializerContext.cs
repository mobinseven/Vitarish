using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.Assistant.Operations;

namespace Vitarish.Clinic.Agents;

[JsonSerializable(typeof(AssistantSearchResult))]
internal sealed partial class AssistantJsonSerializerContext : JsonSerializerContext;
