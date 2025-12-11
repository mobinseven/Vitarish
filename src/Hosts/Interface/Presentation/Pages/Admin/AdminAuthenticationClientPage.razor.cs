using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.Interface.Presentation.Components.Admin;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Interface.Presentation.Pages.Admin;

public partial class AdminAuthenticationClientPage
{
    [Parameter] public string? ClientId { get; set; }

    private AuthenticationClient? Client;
    private List<User> Users { get; set; } = [];
    private List<AccessUsersMatrix.UserAccess> AccessClaims { get; set; } = [];
    private List<AccessUsersMatrix.UserAccess> OriginalClaims { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            NavigationManager.NavigateTo("/Admin/AuthenticationClients");
            return;
        }

        Client = await AuthenticationClientsService.GetAuthenticationClientAsync(ClientId);
        Users = [.. await UserService.GetUsersAsync()];
        IEnumerable<UserDto> users = await UserAccessService.GetClientUsersAsync(ClientId);
        AccessClaims = users
            .Where(u => u.AccessClaim is not null)
            .Select(u => new AccessUsersMatrix.UserAccess(u.Id, AccessExtensions.GenerateAccess(u.AccessClaim!.Value!)))
            .ToList();
        OriginalClaims = AccessClaims.Select(c => new AccessUsersMatrix.UserAccess(c.UserId, c.Access)).ToList();
    }

    private async Task Save()
    {
        if (Client is null)
        {
            return;
        }

        UpdateAuthenticationClient update = new(Client.Id,
                                                           Client.Url,
                                                           Client.Title,
                                                           Client.Description,
                                                           Client.LogoUrl,
                                                           Client.AutoAccess);
        await AuthenticationClientsService.UpdateAuthenticationClientAsync(update);

        await SyncClaims();
        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    private async Task Delete()
    {
        if (Client is null)
        {
            return;
        }

        await AuthenticationClientsService.DeleteAuthenticationClientAsync(new DeleteAuthenticationClient(Client.Id));
        NavigationManager.NavigateTo("/Admin/AuthenticationClients");
    }

    private async Task SyncClaims()
    {
        List<AccessUsersMatrix.UserAccess> added = AccessClaims.Where(ac => OriginalClaims.All(oc => oc.UserId != ac.UserId || oc.Access.Value != ac.Access.Value)).ToList();
        List<AccessUsersMatrix.UserAccess> removed = OriginalClaims.Where(oc => AccessClaims.All(ac => ac.UserId != oc.UserId)).ToList();

        foreach (AccessUsersMatrix.UserAccess add in added)
        {
            await UserAccessService.SetUserAccessAsync(new SetUserAccessClaimCommand(add.UserId, Client!.Id, add.Access.Value!));
        }

        foreach (AccessUsersMatrix.UserAccess rem in removed)
        {
            await UserAccessService.RemoveUserAccessAsync(new RemoveUserAccessClaimCommand(rem.UserId, Client!.Id));
        }

        OriginalClaims = AccessClaims
            .Select(c => new AccessUsersMatrix.UserAccess(c.UserId, c.Access))
            .ToList();
    }
}
