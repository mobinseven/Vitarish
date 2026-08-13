using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.ClinicWorkspace;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

[JsonSerializable(typeof(ClinicWorkspaceAction))]
[JsonSerializable(typeof(ClinicWorkspaceActionFailure))]
internal sealed partial class ClinicWorkspaceActionJsonSerializerContext : JsonSerializerContext;
