using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.Assistant;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

[JsonSerializable(typeof(AssistantAction))]
[JsonSerializable(typeof(AssistantActionFailure))]
internal sealed partial class AssistantActionJsonSerializerContext : JsonSerializerContext;
