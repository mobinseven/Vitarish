using MediatR;
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
    private string? _errorMessage;

    private string? _localEntityType;
    private string? _localEntityIdText;
    private string? _searchText;
    private bool _includeArchived;

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

    private async Task OpenLinkDialogAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(CreateResourceLinkDialog.InitialLocalEntityType), _localEntityType },
            { nameof(CreateResourceLinkDialog.InitialLocalEntityId), _localEntityIdText }
        };

        IDialogReference dialog = await DialogService.ShowAsync<CreateResourceLinkDialog>(
            Localizer["AddLink"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadLinksAsync();
        }
    }

    private async Task OpenLinkDetailAsync(ResourceLinkReadDto link)
    {
        DialogParameters parameters = new()
        {
            { nameof(ResourceLinkDetailDialog.Link), link }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ResourceLinkDetailDialog>(
            Localizer["ResourceLink"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadLinksAsync();
        }
    }
}
