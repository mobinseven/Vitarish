namespace Vitarish.Clients.Interface.Sea.Components.Events.Plugins;
/// <summary>
/// Describes a plugin that can provide a custom UI flow for creating events.
/// Implementations are resolved through dependency injection and rendered by
/// the events grid when a user starts a new event.
/// </summary>
public interface IEventCreationFormPlugin : IPlugin<EventCreationFormContext> { }
