namespace Vitarish.Clients.Interface.Sea.Components.Procedures.Plugins;
/// <summary>
/// Carries runtime information about a procedure creation request when invoking
/// <see cref="IProcedureCreationFormPlugin"/> instances.
/// </summary>
/// <param name="Services">The scoped service provider resolved for the dialog lifetime.</param>
/// <param name="Project">The project that will own the procedure created by the plugin.</param>
/// <param name="Metadata">Additional values describing the request, such as parent entity identifiers.</param>
public sealed record ProcedureCreationFormContext(
    IServiceProvider Services,
    ProjectDto Project,
    IReadOnlyDictionary<string, object?> Metadata) : PluginContext(Services, Metadata)
{ }
