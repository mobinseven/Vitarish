using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminOwnersPage
{
    private List<OwnerReadDto> Owners { get; set; } = [];
    private string newOwnerName = string.Empty, newOwnerLogoUrl = string.Empty, newOwnerDescription = string.Empty;
    private OwnerReadDto? editingOwner;

    protected override async Task OnInitializedAsync() => Owners = [.. await PartiesService.GetOwnersAsync()];

    private void RowClicked(OwnerReadDto item)
    {
        editingOwner = item;
        newOwnerName = editingOwner.Name;
        newOwnerLogoUrl = editingOwner.LogoUrl ?? string.Empty;
        newOwnerDescription = editingOwner.Description ?? string.Empty;
    }

    private async Task SaveOwner()
    {
        if (editingOwner is null)
        {
            OwnerWriteDto dto = new()
            {
                Id = Guid.NewGuid(),
                Name = newOwnerName,
                LogoUrl = string.IsNullOrWhiteSpace(newOwnerLogoUrl) ? null : newOwnerLogoUrl,
                Description = string.IsNullOrWhiteSpace(newOwnerDescription) ? null : newOwnerDescription
            };
            OwnerReadDto? owner = await PartiesService.CreateOwnerAsync(dto);
            if (owner is not null)
            {
                Owners.Add(owner);
            }
        }
        else
        {
            OwnerWriteDto dto = new()
            {
                Id = editingOwner.Id,
                Name = newOwnerName,
                LogoUrl = string.IsNullOrWhiteSpace(newOwnerLogoUrl) ? null : newOwnerLogoUrl,
                Description = string.IsNullOrWhiteSpace(newOwnerDescription) ? null : newOwnerDescription
            };
            OwnerReadDto? updated = await PartiesService.UpdateOwnerAsync(dto);
            if (updated is not null)
            {
                int idx = Owners.FindIndex(o => o.Id == updated.Id);
                if (idx >= 0)
                {
                    Owners[idx] = updated;
                }
            }
            editingOwner = null;
        }

        newOwnerName = string.Empty;
        newOwnerLogoUrl = string.Empty;
        newOwnerDescription = string.Empty;
        StateHasChanged();
    }

    private async Task RemoveOwner(OwnerReadDto owner)
    {
        await PartiesService.DeleteOwnerAsync(owner.Id);
        _ = Owners.Remove(owner);
        if (editingOwner?.Id == owner.Id)
        {
            CancelEditOwner();
        }
        StateHasChanged();
    }

    private void CancelEditOwner()
    {
        editingOwner = null;
        newOwnerName = string.Empty;
        newOwnerLogoUrl = string.Empty;
        newOwnerDescription = string.Empty;
    }
}
