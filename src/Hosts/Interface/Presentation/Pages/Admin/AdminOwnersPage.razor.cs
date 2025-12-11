
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Interface.Presentation.Pages.Admin;

public partial class AdminOwnersPage
{
    private List<Owner> Owners { get; set; } = [];
    private string newOwnerName = string.Empty, newOwnerLogoUrl = string.Empty, newOwnerDescription = string.Empty;
    private Owner? editingOwner;

    protected override async Task OnInitializedAsync() => Owners = [.. await PartiesService.GetOwnersAsync()];

    private void RowClicked(DataGridRowClickEventArgs<Owner> args)
    {
        editingOwner = args.Item;
        newOwnerName = editingOwner.Name;
        newOwnerLogoUrl = editingOwner.LogoUrl ?? string.Empty;
        newOwnerDescription = editingOwner.Description ?? string.Empty;
    }

    private async Task SaveOwner()
    {
        if (editingOwner is null)
        {
            CreateOwner request = new(
                newOwnerName,
                string.IsNullOrWhiteSpace(newOwnerLogoUrl) ? null : newOwnerLogoUrl,
                string.IsNullOrWhiteSpace(newOwnerDescription) ? null : newOwnerDescription);
            Owner? owner = await PartiesService.CreateOwnerAsync(request);
            if (owner is not null)
            {
                Owners.Add(owner);
            }
        }
        else
        {
            UpdateOwner request = new(
                editingOwner.Id,
                newOwnerName,
                string.IsNullOrWhiteSpace(newOwnerLogoUrl) ? null : newOwnerLogoUrl,
                string.IsNullOrWhiteSpace(newOwnerDescription) ? null : newOwnerDescription);
            Owner? updated = await PartiesService.UpdateOwnerAsync(request);
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

    private async Task RemoveOwner(Owner owner)
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
