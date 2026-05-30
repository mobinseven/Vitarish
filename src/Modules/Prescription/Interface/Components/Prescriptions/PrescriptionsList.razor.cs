namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionsList : PrescriptionBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public Guid? PatientId { get; set; }

    private List<PrescriptionSummaryDto> prescriptions = [];
    private Guid? previousPatientId;
    private Guid? previousClinicId;

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task OnParametersSetAsync()
    {
        Guid? clinicId = GetClinicId();
        if (PatientId != previousPatientId || clinicId != previousClinicId)
        {
            previousPatientId = PatientId;
            previousClinicId = clinicId;
            await ReloadRequired();
        }
    }

    protected override async Task ReloadRequired()
    {
        Guid? clinicId = GetClinicId();

        if (PatientId is null && clinicId is null)
        {
            prescriptions = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        IReadOnlyList<PrescriptionSummaryDto> items = await Mediator.Send(new GetPrescriptionHistoryQuery
        {
            PatientId = PatientId,
            ClinicId = PatientId is null ? clinicId : null,
            Skip = 0,
            Take = 50
        });

        prescriptions = [.. items.OrderByDescending(p => p.CreatedAt)];
        await InvokeAsync(StateHasChanged);
    }

    private Guid? GetClinicId() =>
        Guid.TryParse(RequestContext.TenantId, out Guid clinicId) ? clinicId : null;

    private static string FormatLabel(PrescriptionSummaryDto p) =>
        $"{p.CreatedAt.ToLocalTime():d} — {p.SelectedProviderKey ?? "—"} ({p.ItemCount})";
}
