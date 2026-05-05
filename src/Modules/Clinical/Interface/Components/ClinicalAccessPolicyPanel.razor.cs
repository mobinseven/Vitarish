using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalAccessPolicyPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private ClinicalAccessPolicy? _policy;
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task ReloadRequired() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _policy = await Mediator.Send(new GetAccessPolicyQuery());
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
