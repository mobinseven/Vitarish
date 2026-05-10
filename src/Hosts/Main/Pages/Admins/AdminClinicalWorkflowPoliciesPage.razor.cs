using MediatR;
using MudBlazor;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminClinicalWorkflowPoliciesPage
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private IReadOnlyList<WorkflowPolicyReadDto> _policies = [];
    private bool _loading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _policies = await Mediator.Send(new ListWorkflowPoliciesQuery(RequestContext.TenantId ?? string.Empty));
        }
        finally
        {
            _loading = false;
        }
    }

    private Task EditPolicyAsync(WorkflowPolicyReadDto policy)
    {
        return Task.CompletedTask;
    }
}
