using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalResourceLinksPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter] public string? InitialLocalEntityType { get; set; }
    [Parameter] public Guid? InitialLocalEntityId { get; set; }

    private IReadOnlyList<ResourceLinkReadDto> _links = [];
    private bool _loading;
    private bool _linking;
    private bool _showLinkDialog;
    private string? _errorMessage;
    private string? _linkErrorMessage;

    private string? _localEntityType;
    private string? _localEntityIdText;
    private string? _searchText;
    private bool _includeArchived;

    private string? _newLocalEntityType;
    private string? _newLocalEntityIdText;
    private string? _newResourceType;
    private string? _newResourceId;
    private string? _newPatientReference;

    protected override async Task ReloadRequired()
    {
        _localEntityType ??= InitialLocalEntityType;
        _localEntityIdText ??= InitialLocalEntityId?.ToString();
        await LoadLinksAsync();
    }

    private async Task LoadLinksAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            Guid? localEntityId = Guid.TryParse(_localEntityIdText, out Guid parsedId) ? parsedId : null;

            _links = await Mediator.Send(new GetResourceLinksQuery(new ResourceLinkFilter
            {
                IncludeArchived = _includeArchived,
                LocalEntityId = localEntityId,
                LocalEntityType = _localEntityType,
                SearchText = _searchText,
                Take = 100
            }));
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void OpenLinkDialog()
    {
        _newLocalEntityType = _localEntityType;
        _newLocalEntityIdText = _localEntityIdText;
        _newResourceType = null;
        _newResourceId = null;
        _newPatientReference = null;
        _linkErrorMessage = null;
        _showLinkDialog = true;
    }

    private async Task ConfirmLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(_newLocalEntityType) || string.IsNullOrWhiteSpace(_newLocalEntityIdText) ||
            string.IsNullOrWhiteSpace(_newResourceType) || string.IsNullOrWhiteSpace(_newResourceId))
        {
            _linkErrorMessage = Localizer["AllRequiredFieldsMustBeFilled"];
            return;
        }

        if (!Guid.TryParse(_newLocalEntityIdText, out Guid localEntityId))
        {
            _linkErrorMessage = Localizer["InvalidLocalEntityId"];
            return;
        }

        _linking = true;
        _linkErrorMessage = null;

        try
        {
            _ = await Mediator.Send(new CreateResourceLinkCommand(new ResourceLinkWriteDto
            {
                LocalEntityId = localEntityId,
                LocalEntityType = _newLocalEntityType,
                PatientReference = _newPatientReference,
                ResourceId = _newResourceId,
                ResourceType = _newResourceType
            }));

            _showLinkDialog = false;
            await LoadLinksAsync();
        }
        catch (Exception exception)
        {
            _linkErrorMessage = exception.Message;
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
