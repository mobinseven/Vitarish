using Vitarish.Entities.Server.Models;

namespace Vitarish.Medical.Interface.Components.Curation;

public partial class CodeMapForm : MedicalBase
{
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private List<CodeSystem> codeSystems = [];
    private string sourceSystemKey = string.Empty;
    private string targetSystemKey = string.Empty;
    private string sourceCodeText = string.Empty;
    private string targetCodeText = string.Empty;
    private MedicalCode? sourceCode;
    private MedicalCode? targetCode;
    private MappingType mappingType = MappingType.Equivalent;
    private DateOnly? effectiveFrom;
    private DateOnly? effectiveTo;

    protected override async Task ReloadRequired()
    {
        codeSystems = [.. await QueryService.ListCodeSystemsAsync(activeOnly: true)];
        if (codeSystems.Count > 0)
        {
            sourceSystemKey = string.IsNullOrWhiteSpace(sourceSystemKey) ? codeSystems[0].SystemKey : sourceSystemKey;
            targetSystemKey = string.IsNullOrWhiteSpace(targetSystemKey) ? codeSystems[0].SystemKey : targetSystemKey;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task FindSourceCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(sourceSystemKey) || string.IsNullOrWhiteSpace(sourceCodeText))
        {
            return;
        }

        sourceCode = await QueryService.ResolveMedicalCodeAsync(sourceSystemKey, sourceCodeText);
        await InvokeAsync(StateHasChanged);
    }

    private async Task FindTargetCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(targetSystemKey) || string.IsNullOrWhiteSpace(targetCodeText))
        {
            return;
        }

        targetCode = await QueryService.ResolveMedicalCodeAsync(targetSystemKey, targetCodeText);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveAsync()
    {
        if (sourceCode is null || targetCode is null)
        {
            return;
        }

        if (effectiveFrom > effectiveTo)
        {
            return;
        }

        EntityResult<Guid> result = await Mediator.Send(new CreateCodeMapCommand(
            sourceCode.Id,
            targetCode.Id,
            mappingType,
            1.0m,
            effectiveFrom,
            effectiveTo,
            null));

        if (result.IsSuccess)
        {
            await OnSaved.InvokeAsync(result.Value);
        }
    }
}
