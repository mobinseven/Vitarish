namespace Vitarish.Medical.Interface.Components.Imports;

public partial class ImportCodeSystemReleasePanel : MedicalBase
{
    [Parameter] public EventCallback<ImportResult> OnImported { get; set; }

    private List<CodeSystem> codeSystems = [];
    private ImportResult? lastResult;
    private bool running;
    private readonly ImportWriteDto writeDto = new();

    protected override async Task ReloadRequired()
    {
        codeSystems = [.. await QueryService.ListCodeSystemsAsync(activeOnly: false)];
        if (writeDto.CodeSystemId == Guid.Empty && codeSystems.Count > 0)
        {
            writeDto.CodeSystemId = codeSystems[0].Id;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task RunAsync()
    {
        if (writeDto.CodeSystemId == Guid.Empty || string.IsNullOrWhiteSpace(writeDto.ReleaseName))
        {
            return;
        }

        if (writeDto.EffectiveFrom > writeDto.EffectiveTo)
        {
            return;
        }

        running = true;
        lastResult = null;
        await InvokeAsync(StateHasChanged);

        var result = await Mediator.Send(new ImportCodeSystemReleaseCommand(
            writeDto.CodeSystemId,
            writeDto.ReleaseName,
            null,
            writeDto.EffectiveFrom,
            writeDto.EffectiveTo,
            writeDto.SourceLocator ?? string.Empty,
            writeDto.SourceVersion ?? string.Empty,
            writeDto.SourceChecksum,
            writeDto.SourceType,
            [],
            writeDto.DryRun));

        running = false;

        if (result.IsSuccess && result.Value is not null)
        {
            lastResult = result.Value;
            await OnImported.InvokeAsync(result.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    private sealed class ImportWriteDto
    {
        public Guid CodeSystemId { get; set; }
        public string ReleaseName { get; set; } = string.Empty;
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public string? SourceLocator { get; set; }
        public string? SourceVersion { get; set; }
        public string? SourceChecksum { get; set; }
        public ImportSourceType SourceType { get; set; }
        public bool DryRun { get; set; }
    }
}
