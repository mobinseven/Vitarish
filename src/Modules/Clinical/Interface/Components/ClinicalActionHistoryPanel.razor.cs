using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalActionHistoryPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private IReadOnlyList<ClinicalActionRecordReadDto> _records = [];
    private bool _loading;

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _records = await Mediator.Send(new ListClinicalActionRecordsQuery(new ClinicalActionRecordFilter
            {
                WorkflowId = WorkflowId,
                Take = 50
            }));
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
