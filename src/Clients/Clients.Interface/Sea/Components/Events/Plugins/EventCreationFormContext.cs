namespace Vitarish.Clients.Interface.Sea.Components.Events.Plugins;
/// <summary>
/// Carries runtime information about an event creation request when invoking
/// <see cref="IEventCreationFormPlugin"/> instances.
/// </summary>
/// <param name="Services">The scoped service provider resolved for the dialog lifetime.</param>
/// <param name="Project">The project that owns the procedure receiving the new event.</param>
/// <param name="Procedure">The procedure that will own the event created by the plugin.</param>
/// <param name="PreviousEventId">The event that will precede the new event in the chain, if any.</param>
/// <param name="Metadata">Additional values describing the request, such as parent identifiers or feature flags.</param>
public sealed record EventCreationFormContext(
    IServiceProvider Services,
    ProjectDto Project,
    ProcedureDto Procedure,
    Guid? PreviousEventId,
    IReadOnlyDictionary<string, object?> Metadata) : PluginContext(Services, Metadata)
{ }
