using MediatR;
using Vitarish.Clinical.Application.Queries;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalAuditLogPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IReadOnlyList<AuditLogReadDto> _entries = [];
    private bool _loading;

    private string? _category;
    private string? _action;
    private string? _resourceType;
    private string? _actorId;

    protected override Task ReloadRequired() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            _entries = await Mediator.Send(new GetAuditLogQuery(new AuditLogFilter
            {
                Category = _category,
                Action = _action,
                ResourceType = _resourceType,
                ActorId = _actorId,
                Take = 100
            }));
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }
}
