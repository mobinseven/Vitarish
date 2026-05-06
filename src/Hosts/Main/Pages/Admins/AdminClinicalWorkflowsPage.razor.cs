using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminClinicalWorkflowsPage
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    private VisitWorkflowStatus? _statusFilter;
    private string? _locationFilter;
    private string? _providerFilter;
    private IReadOnlyList<VisitWorkflowReadDto> _workflows = [];
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _workflows = await Mediator.Send(new ListVisitWorkflowsQuery(new VisitWorkflowFilter
            {
                Status = _statusFilter,
                AssignedLocationId = _locationFilter,
                AssignedProviderId = _providerFilter,
                Take = 100
            }));
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CancelWorkflowAsync(Guid workflowId)
    {
        _ = await Mediator.Send(new CancelVisitWorkflowCommand(
            workflowId,
            RequestContext.UserId ?? "admin",
            "Cancelled by admin"));

        await LoadAsync();
    }
}
