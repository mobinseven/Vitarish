using Vitarish.Entities.Server.Models;

namespace Vitarish.Medical.Interface.Components.Curation;

public partial class LocalDrugForm : MedicalBase
{
    [Parameter] public Drug? Drug { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }
    [Parameter] public EventCallback OnRetired { get; set; }

    private DrugWriteDto writeDto = new();

    protected override void OnParametersSet()
    {
        writeDto = Drug is not null
            ? new DrugWriteDto
            {
                PrimaryName = Drug.PrimaryName,
                PrimaryIdentifierType = Drug.PrimaryIdentifierType,
                PrimaryIdentifierValue = Drug.PrimaryIdentifierValue,
                EffectiveFrom = Drug.EffectiveFrom,
                EffectiveTo = Drug.EffectiveTo
            }
            : new DrugWriteDto();
    }

    private async Task SaveAsync()
    {
        if (Drug is null)
        {
            EntityResult<Guid> result = await Mediator.Send(new CreateDrugCommand(
                writeDto.PrimaryName,
                writeDto.PrimaryIdentifierType,
                writeDto.PrimaryIdentifierValue,
                writeDto.EffectiveFrom,
                writeDto.EffectiveTo));

            if (result.IsSuccess)
            {
                await OnSaved.InvokeAsync(result.Value);
            }
        }
        else
        {
            EntityResult<Guid> result = await Mediator.Send(new UpdateLocalDrugCommand(
                Drug.Id,
                writeDto.PrimaryName,
                writeDto.EffectiveFrom,
                writeDto.EffectiveTo));

            if (result.IsSuccess)
            {
                await OnSaved.InvokeAsync(result.Value);
            }
        }
    }

    private async Task RetireAsync()
    {
        if (Drug is null)
        {
            return;
        }

        EntityResult<Guid> result = await Mediator.Send(new RetireDrugCommand(Drug.Id));
        if (result.IsSuccess)
        {
            await OnRetired.InvokeAsync();
        }
    }

    private sealed class DrugWriteDto
    {
        public string PrimaryName { get; set; } = string.Empty;
        public DrugIdentifierType PrimaryIdentifierType { get; set; }
        public string PrimaryIdentifierValue { get; set; } = string.Empty;
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
