using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components.Nurse;

public partial class NurseTriageQueuePanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter] public string? LocationId { get; set; }
    [Parameter] public EventCallback<OperationalProjectionSummary> OnSelected { get; set; }

    private IReadOnlyList<OperationalProjectionSummary> _queue = [];
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _queue = await Mediator.Send(new GetVisitQueueQuery("Nurse", LocationId));
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
