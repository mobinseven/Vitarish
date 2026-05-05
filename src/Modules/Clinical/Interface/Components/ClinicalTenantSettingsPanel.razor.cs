using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalTenantSettingsPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private ClinicalTenantSettings? _settings;
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task ReloadRequired() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _settings = await Mediator.Send(new GetTenantClinicalSettingsQuery());
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
