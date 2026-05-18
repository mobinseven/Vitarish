namespace Vitarish.Clinical.Interface.Plugins;

public interface IClinicalProcedureSectionPlugin
{
    string Id { get; }
    string Label { get; }
    RenderFragment Render(ProjectReadDto patient, ProcedureReadDto procedure);
}
