namespace Vitarish.Clients.Interface.Sea.Components.Projects.Plugins;
/// <summary>
/// Describes a plugin that can supply a custom UI flow for creating projects.
/// Implementations are discovered through dependency injection and rendered dynamically by
/// the project grid when users start a new project.
/// </summary>
public interface IProjectCreationFormPlugin : IPlugin<ProjectCreationFormContext>
{ }
