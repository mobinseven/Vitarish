using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Nurse;

public partial class VitalsEntryDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private decimal? _systolicBp;
    private decimal? _diastolicBp;
    private decimal? _pulseRate;
    private decimal? _temperature;
    private decimal? _weightKg;
    private decimal? _heightCm;
    private decimal? _spo2;
    private decimal? _respRate;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new RecordVitalsCommand(
                WorkflowId,
                new VitalsPayload
                {
                    SystolicBp = _systolicBp,
                    DiastolicBp = _diastolicBp,
                    PulseRate = _pulseRate,
                    Temperature = _temperature,
                    WeightKg = _weightKg,
                    HeightCm = _heightCm,
                    SpO2Percent = _spo2,
                    RespiratoryRate = _respRate
                },
                RequestContext.UserId ?? string.Empty));

            if (result.IsSuccess)
            {
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _errorMessage = result.ErrorMessage ?? result.ValidationErrors.FirstOrDefault()?.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
