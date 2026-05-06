using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class VisitSummaryPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private VisitWorkflowReadDto? _workflow;
    private bool _loading;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _workflow = await Mediator.Send(new GetVisitWorkflowQuery(WorkflowId));
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
