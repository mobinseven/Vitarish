namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionsList : PrescriptionBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public Guid? PatientId { get; set; }

    private List<PrescriptionSummaryDto> prescriptions = [];
    private Guid? previousPatientId;

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task OnParametersSetAsync()
    {
        if (PatientId != previousPatientId)
        {
            previousPatientId = PatientId;
            await ReloadRequired();
        }
    }

    protected override async Task ReloadRequired()
    {
        if (PatientId is null)
        {
            prescriptions = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        IReadOnlyList<PrescriptionSummaryDto> items = await Mediator.Send(new GetPrescriptionHistoryQuery
        {
            PatientId = PatientId.Value,
            Skip = 0,
            Take = 50
        });

        prescriptions = [.. items.OrderByDescending(p => p.CreatedAt)];
        await InvokeAsync(StateHasChanged);
    }

    private static string FormatLabel(PrescriptionSummaryDto p) =>
        $"{p.CreatedAt.ToLocalTime():d} — {p.SelectedProviderKey ?? "—"} ({p.ItemCount})";
}
