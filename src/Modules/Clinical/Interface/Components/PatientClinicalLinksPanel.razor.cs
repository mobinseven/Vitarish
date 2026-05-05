using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class PatientClinicalLinksPanel : CommonBase
{
    private const string LocalEntityType = "Project";

    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public Guid PatientProjectId { get; set; }
    [CascadingParameter] private bool IsAdmin { get; set; }

    private IReadOnlyList<ResourceLinkReadDto> _links = [];
    private bool _loaded;
    private bool _linking;
    private bool _showLinkDialog;
    private bool _hasLinks;
    private string? _errorMessage;
    private string? _linkError;

    private string? _newResourceType;
    private string? _newResourceId;
    private string? _newPatientReference;

    protected override async Task ReloadRequired()
    {
        await LoadLinksAsync();
    }

    private async Task LoadLinksAsync()
    {
        try
        {
            _links = await Mediator.Send(new GetResourceLinksQuery(new ResourceLinkFilter
            {
                LocalEntityType = LocalEntityType,
                LocalEntityId = PatientProjectId,
                Take = 50
            }));

            _hasLinks = _links.Count > 0;
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _loaded = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void OpenLinkDialog()
    {
        _newResourceType = "Patient";
        _newResourceId = null;
        _newPatientReference = null;
        _linkError = null;
        _showLinkDialog = true;
    }

    private async Task ConfirmLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(_newResourceType) || string.IsNullOrWhiteSpace(_newResourceId))
        {
            _linkError = Localizer["ResourceTypeAndIdRequired"];
            return;
        }

        _linking = true;
        _linkError = null;

        try
        {
            _ = await Mediator.Send(new CreateResourceLinkCommand(new ResourceLinkWriteDto
            {
                LocalEntityId = PatientProjectId,
                LocalEntityType = LocalEntityType,
                ResourceId = _newResourceId,
                ResourceType = _newResourceType,
                PatientReference = _newPatientReference
            }));

            _showLinkDialog = false;
            await LoadLinksAsync();
        }
        catch (Exception exception)
        {
            _linkError = exception.Message;
        }
        finally
        {
            _linking = false;
        }
    }

    private async Task UnlinkAsync(Guid id)
    {
        _errorMessage = null;

        try
        {
            await Mediator.Send(new DeleteResourceLinkCommand(id));
            await LoadLinksAsync();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
    }
}
