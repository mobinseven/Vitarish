namespace Vitarish.Clinical.Interface.Plugins;

public interface IClinicalPatientSectionPlugin
{
    string Id { get; }
    string Label { get; }
    RenderFragment Render(ProjectReadDto patient);
}
