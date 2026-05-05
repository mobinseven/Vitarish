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
    private bool _hasLinks;
    private string? _errorMessage;

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

    private async Task OpenLinkDialogAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(LinkFhirResourceDialog.PatientProjectId), PatientProjectId }
        };

        IDialogReference dialog = await DialogService.ShowAsync<LinkFhirResourceDialog>(
            Localizer["LinkFhirResource"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadLinksAsync();
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
