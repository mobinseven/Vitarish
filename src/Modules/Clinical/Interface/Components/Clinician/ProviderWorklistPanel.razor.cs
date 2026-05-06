using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class ProviderWorklistPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public string ProviderId { get; set; } = string.Empty;
    [Parameter] public EventCallback<OperationalProjectionSummary> OnSelected { get; set; }

    private IReadOnlyList<OperationalProjectionSummary> _worklist = [];
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _worklist = await Mediator.Send(new GetProviderWorklistQuery(ProviderId));
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
