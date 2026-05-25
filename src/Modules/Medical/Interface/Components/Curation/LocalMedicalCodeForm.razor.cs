using Vitarish.Entities.Server.Models;

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
        var systems = await QueryService.ListCodeSystemsAsync(activeOnly: true);
        var allReleases = new List<CodeSystemRelease>();
        foreach (var cs in systems)
        {
            allReleases.AddRange(await QueryService.ListReleasesAsync(cs.Id, activeOnly: true));
        }

        releases = allReleases;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveAsync()
    {
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
