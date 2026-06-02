namespace Vitarish.Medical.Interface.Components.Curation;

public partial class LocalMedicalCodeForm : MedicalBase
{
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private List<CodeSystemRelease> releases = [];
    private readonly LocalCodeWriteDto writeDto = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<CodeSystemRelease>();
    }

    protected override async Task ReloadRequired()
    {
        IReadOnlyList<CodeSystem> systems = [.. await QueryService.ListCodeSystemsAsync(activeOnly: true)];
        Task<IReadOnlyList<CodeSystemRelease>>[] releaseTasks = [.. systems.Select(cs => QueryService.ListReleasesAsync(cs.Id, activeOnly: true))];
        IReadOnlyList<CodeSystemRelease>[] releaseResults = await Task.WhenAll(releaseTasks);

        releases = [.. releaseResults.SelectMany(static result => result)];
        if (writeDto.CodeSystemReleaseId == Guid.Empty && releases.Count > 0)
        {
            writeDto.CodeSystemReleaseId = releases[0].Id;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveAsync()
    {
        if (writeDto.CodeSystemReleaseId == Guid.Empty || string.IsNullOrWhiteSpace(writeDto.Code) || string.IsNullOrWhiteSpace(writeDto.DisplayName))
        {
            return;
        }

        if (writeDto.EffectiveFrom > writeDto.EffectiveTo)
        {
            return;
        }

        EntityResult<Guid> result = await Mediator.Send(new CreateLocalMedicalCodeCommand(
            writeDto.CodeSystemReleaseId,
            writeDto.Code,
            writeDto.DisplayName,
            writeDto.LongDescription,
            writeDto.EffectiveFrom,
            writeDto.EffectiveTo));

        if (result.IsSuccess)
        {
            await OnSaved.InvokeAsync(result.Value);
        }
    }

    private sealed class LocalCodeWriteDto
    {
        public Guid CodeSystemReleaseId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? LongDescription { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
