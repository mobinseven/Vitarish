using MudBlazor;
namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionsList : PrescriptionBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public Guid? PatientId { get; set; }

    private List<PrescriptionSummaryDto> prescriptions = [];
    private Guid? previousPatientId;
    private Guid? previousClinicId;

    // Pagination
    private int currentPage;
    private readonly int pageSize = 20;
    private bool hasMore;

    // Filters
    private PrescriptionStatus? statusFilter;
    private string? providerFilter;

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task OnParametersSetAsync()
    {
        Guid? clinicId = GetClinicId();
        if (PatientId != previousPatientId || clinicId != previousClinicId)
        {
            previousPatientId = PatientId;
            previousClinicId = clinicId;
            currentPage = 0;
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
            StatusFilter = statusFilter,
            ProviderKeyFilter = providerFilter,
            Skip = currentPage * pageSize,
            Take = pageSize + 1
        });

        hasMore = items.Count > pageSize;
        prescriptions = [.. items.Take(pageSize).OrderByDescending(p => p.CreatedAt)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnStatusFilterChanged(PrescriptionStatus? value)
    {
        statusFilter = value;
        currentPage = 0;
        await ReloadRequired();
    }

    private async Task OnProviderFilterChanged(string? value)
    {
        providerFilter = value;
        currentPage = 0;
        await ReloadRequired();
    }

    private async Task NextPageAsync()
    {
        currentPage++;
        await ReloadRequired();
    }

    private async Task PreviousPageAsync()
    {
        if (currentPage > 0)
        {
            currentPage--;
            await ReloadRequired();
        }
    }

    private Guid? GetClinicId() =>
        Guid.TryParse(RequestContext.TenantId, out Guid clinicId) ? clinicId : null;

    private static string FormatLabel(PrescriptionSummaryDto p) =>
        $"{p.CreatedAt.ToLocalTime():d} — {p.SelectedProviderKey ?? "—"} ({p.ItemCount})";
}
