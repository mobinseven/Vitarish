using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components.Receptionist;

public partial class PatientSearchPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter] public EventCallback<VisitWorkflowReadDto> OnSelected { get; set; }

    private string? _searchTerm;
    private IReadOnlyList<VisitWorkflowReadDto> _results = [];
    private bool _loading;
    private bool _searched;

    private async Task OnKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            return;
        }

        _loading = true;
        _searched = false;
        try
        {
            _results = await Mediator.Send(new ListVisitWorkflowsQuery(new VisitWorkflowFilter
            {
                PatientFhirReference = _searchTerm.Trim(),
                Take = 20
            }));
            _searched = true;
        }
        finally
        {
            _loading = false;
        }
    }
}
