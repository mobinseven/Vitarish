namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemReleaseForm : MedicalBase
{
    [Parameter, EditorRequired] public Guid CodeSystemId { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private readonly ReleaseWriteDto writeDto = new();

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(writeDto.ReleaseName))
        {
            return;
        }

        if (writeDto.EffectiveFrom > writeDto.EffectiveTo)
        {
            return;
        }

        EntityResult<Guid> result = await Mediator.Send(new RegisterCodeSystemReleaseCommand(
            CodeSystemId,
            writeDto.ReleaseName,
            writeDto.ReleaseDate,
            writeDto.EffectiveFrom,
            writeDto.EffectiveTo,
            writeDto.SourceLocator,
            writeDto.SourceVersion));

        if (result.IsSuccess)
        {
            await OnSaved.InvokeAsync(result.Value);
        }
    }

    private sealed class ReleaseWriteDto
    {
        public string ReleaseName { get; set; } = string.Empty;
        public DateOnly? ReleaseDate { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string? SourceLocator { get; set; }
        public string? SourceVersion { get; set; }
    }
}
