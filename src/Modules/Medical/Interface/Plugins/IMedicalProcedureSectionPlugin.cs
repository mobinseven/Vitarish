namespace Vitarish.Medical.Interface.Plugins;

public interface IMedicalProcedureSectionPlugin
{
    string Id { get; }
    string Label { get; }
    RenderFragment Render(ProjectReadDto patient, ProcedureReadDto procedure);
}
