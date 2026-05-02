namespace Vitarish.Medical.Interface.Plugins;

public interface IMedicalPatientSectionPlugin
{
    string Id { get; }
    string Label { get; }
    RenderFragment Render(ProjectReadDto patient);
}
