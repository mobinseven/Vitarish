using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminOwnersPage
{
    private List<OwnerReadDto> Owners { get; set; } = [];

    protected override async Task OnInitializedAsync() => Owners = [.. await PartiesService.GetOwnersAsync()];

    private async void Add()
    {
        OwnerReadDto? added = await PartiesService.CreateOwnerAsync(new OwnerWriteDto { Name = Localizer["NewItem", Localizer["Owner"]] });
        if (added is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Error);
            return;
        }
        NavigationManager.NavigateTo($"/Admin/Owners/{added.Id}");
    }
}
