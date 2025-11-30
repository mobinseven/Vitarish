namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Presentation.Abstractions;
using Vorn.Aaas.Parties.Presentation.Contracts;

public partial class AdminOwnersPage
{
    List<Owner> Owners { get; set; } = [];
    string newOwnerName = string.Empty, newOwnerLogoUrl = string.Empty, newOwnerDescription = string.Empty;
    Owner? editingOwner;

    protected override async Task OnInitializedAsync() => Owners = [.. await PartiesService.GetOwnersAsync()];

    void RowClicked(DataGridRowClickEventArgs<Owner> args)
    {
        editingOwner = args.Item;
        newOwnerName = editingOwner.Name;
        newOwnerLogoUrl = editingOwner.LogoUrl ?? string.Empty;
        newOwnerDescription = editingOwner.Description ?? string.Empty;
    }

    async Task SaveOwner()
    {
        if (editingOwner is null)
        {
            var request = new CreateOwnerRequest(
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
            var request = new UpdateOwnerRequest(
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

    async Task RemoveOwner(Owner owner)
    {
        await PartiesService.DeleteOwnerAsync(owner.Id);
        Owners.Remove(owner);
        if (editingOwner?.Id == owner.Id)
        {
            CancelEditOwner();
        }
        StateHasChanged();
    }

    void CancelEditOwner()
    {
        editingOwner = null;
        newOwnerName = string.Empty;
        newOwnerLogoUrl = string.Empty;
        newOwnerDescription = string.Empty;
    }
}
