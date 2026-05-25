using Vitarish.Entities.Server.Models;

namespace Vitarish.Medical.Interface.Components.Curation;

public partial class LocalMedicalServiceForm : MedicalBase
{
    [Parameter] public MedicalService? Service { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }
    [Parameter] public EventCallback OnRetired { get; set; }

    private ServiceWriteDto writeDto = new();

    protected override void OnParametersSet()
    {
        writeDto = Service is not null
            ? new ServiceWriteDto
            {
                Name = Service.Name,
                Category = Service.Category,
                Specialty = Service.Specialty,
                Description = Service.Description,
                EffectiveFrom = Service.EffectiveFrom,
                EffectiveTo = Service.EffectiveTo
            }
            : new ServiceWriteDto();
    }

    private async Task SaveAsync()
    {
        if (Service is null)
        {
            EntityResult<Guid> result = await Mediator.Send(new CreateMedicalServiceCommand(
                writeDto.Name,
                writeDto.Category,
                writeDto.Specialty,
                writeDto.Description,
                writeDto.EffectiveFrom,
                writeDto.EffectiveTo));

            if (result.IsSuccess)
            {
                await OnSaved.InvokeAsync(result.Value);
            }
        }
        else
        {
            EntityResult<Guid> result = await Mediator.Send(new UpdateLocalMedicalServiceCommand(
                Service.Id,
                writeDto.Name,
                writeDto.Category,
                writeDto.Specialty,
                writeDto.Description,
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
        if (Service is null)
        {
            return;
        }

        EntityResult<Guid> result = await Mediator.Send(new RetireMedicalServiceCommand(Service.Id));
        if (result.IsSuccess)
        {
            await OnRetired.InvokeAsync();
        }
    }

    private sealed class ServiceWriteDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Specialty { get; set; }
        public string? Description { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
