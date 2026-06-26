using Vitarish.Interfaces.Admins.Components;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminOwnersPage : PlatformAdminCommonBase
{
    private List<OwnerReadDto> Owners { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        PartiesResult<IReadOnlyList<OwnerReadDto>> result = await PartiesClient.GetOwnersAsync();
        Owners = result.IsSuccess && result.Value is not null ? [.. result.Value] : [];
    }

    private async void Add()
    {
        PartiesResult<OwnerReadDto> addedResult = await PartiesClient.CreateOwnerAsync(new OwnerWriteDto { Name = Localizer["NewItem", Localizer["Owner"]] });
        if (!addedResult.IsSuccess || addedResult.Value is null)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
            return;
        }
        NavigationManager.NavigateTo(AppRoutes.PlatformAdmin.Owner(addedResult.Value.Id));
    }
}
