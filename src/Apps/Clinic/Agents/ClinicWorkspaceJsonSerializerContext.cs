using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;

namespace Vitarish.Clinic.Agents;

[JsonSerializable(typeof(ClinicWorkspaceSearchResult))]
internal sealed partial class ClinicWorkspaceJsonSerializerContext : JsonSerializerContext;
