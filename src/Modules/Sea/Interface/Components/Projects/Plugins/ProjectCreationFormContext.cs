namespace Vitarish.Sea.Interface.Components.Projects.Plugins;
/// <summary>
/// Carries runtime information about a project creation request when invoking
/// <see cref="IProjectCreationFormPlugin"/> instances.
/// </summary>
/// <param name="Services">The scoped service provider resolved for the dialog lifetime.</param>
/// <param name="TemplateId">The optional template requested by the caller.</param>
/// <param name="Metadata">Additional values describing the request, such as a parent entity identifier.</param>
public sealed record ProjectCreationFormContext(
    IServiceProvider Services,
    Guid? TemplateId,
    IReadOnlyDictionary<string, object?> Metadata) : PluginContext(Services, Metadata)
{ }
