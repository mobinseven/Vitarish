namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionTemplateEditor : PrescriptionBase
{
    [Parameter] public Guid? TemplateId { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }

    private PrescriptionTemplateDto? template;
    private string name = string.Empty;
    private string? description;
    private TemplateItemInput newItem = new();
    private Guid? loadedTemplateId;

    protected override Task OnParametersSetAsync() => LoadTemplateAsync(force: false);

    protected override Task ReloadRequired() => LoadTemplateAsync(force: true);

    private async Task LoadTemplateAsync(bool force)
    {
        if (!force && TemplateId == loadedTemplateId)
        {
            return;
        }

        loadedTemplateId = TemplateId;

        if (TemplateId is null || TemplateId == Guid.Empty)
        {
            template = null;
            name = string.Empty;
            description = null;
            newItem = new();
            return;
        }

        template = await Mediator.Send(new GetPrescriptionTemplateQuery
        {
            TemplateId = TemplateId.Value,
            TenantId = RequestContext.TenantId ?? string.Empty
        });
        name = template?.Name ?? string.Empty;
        description = template?.Description;
        newItem = new();
    }

    private async Task SaveAsync()
    {
        PrescriptionTemplateCommandResult result = TemplateId is null || TemplateId == Guid.Empty
            ? await Mediator.Send(new CreatePrescriptionTemplateCommand { TenantId = RequestContext.TenantId ?? string.Empty, Name = name, Description = description })
            : await Mediator.Send(new UpdatePrescriptionTemplateCommand { TemplateId = TemplateId.Value, TenantId = RequestContext.TenantId ?? string.Empty, Name = name, Description = description });

        if (!result.IsSuccess || result.Id is null)
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], MudBlazor.Severity.Error);
            return;
        }

        Snackbar.Add(Localizer["Saved"], MudBlazor.Severity.Success);
        await OnSaved.InvokeAsync(result.Id.Value);
    }

    private async Task AddItemAsync()
    {
        if (TemplateId is null || TemplateId == Guid.Empty) return;
        if (string.IsNullOrWhiteSpace(newItem.ReferenceCode) && string.IsNullOrWhiteSpace(newItem.DisplayName)) return;
        PrescriptionTemplateCommandResult result = await Mediator.Send(new AddPrescriptionTemplateItemCommand
        {
            TemplateId = TemplateId.Value,
            TenantId = RequestContext.TenantId ?? string.Empty,
            Category = newItem.Category,
            ReferenceCode = newItem.ReferenceCode,
            DisplayName = newItem.DisplayName,
            DosageJson = newItem.DosageJson,
            Quantity = newItem.Quantity,
            DurationDays = newItem.DurationDays
        });
        if (result.IsSuccess)
        {
            newItem = new();
            await ReloadRequired();
            return;
        }
        Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], MudBlazor.Severity.Error);
    }

    private async Task RemoveItemAsync(Guid itemId)
    {
        if (TemplateId is null || TemplateId == Guid.Empty) return;
        PrescriptionTemplateCommandResult result = await Mediator.Send(new RemovePrescriptionTemplateItemCommand { TemplateId = TemplateId.Value, ItemId = itemId, TenantId = RequestContext.TenantId ?? string.Empty });
        if (result.IsSuccess) await ReloadRequired(); else Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], MudBlazor.Severity.Error);
    }

    private sealed class TemplateItemInput
    {
        public PrescriptionItemCategory Category { get; set; } = PrescriptionItemCategory.Drug;
        public string? ReferenceCode { get; set; }
        public string? DisplayName { get; set; }
        public string? DosageJson { get; set; }
        public int Quantity { get; set; } = 1;
        public int? DurationDays { get; set; }
    }
}
