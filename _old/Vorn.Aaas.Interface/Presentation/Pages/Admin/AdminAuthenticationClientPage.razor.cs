namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

using System.Security.Claims;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Presentation.Services;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Application.Commands;
using Vorn.Aaas.Users.ReadModel.Dtos;
using Vorn.Aaas.Interface.Presentation.Components.Admin;
using Vorn.Aaas.Client.Extensions;

public partial class AdminAuthenticationClientPage
{
    [Parameter] public string? ClientId { get; set; }

    AuthenticationClient? Client;
    List<User> Users { get; set; } = [];
    List<AccessUsersMatrix.UserAccess> AccessClaims { get; set; } = [];
    List<AccessUsersMatrix.UserAccess> OriginalClaims { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if(string.IsNullOrWhiteSpace(ClientId))
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

    async Task Save()
    {
        if(Client is null)
        {
            return;
        }

        var update = new UpdateAuthenticationClientRequest(Client.Id,
                                                           Client.Url,
                                                           Client.Title,
                                                           Client.Description,
                                                           Client.LogoUrl,
                                                           Client.AutoAccess);
        await AuthenticationClientsService.UpdateAuthenticationClientAsync(update);

        await SyncClaims();
        Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    async Task Delete()
    {
        if(Client is null)
        {
            return;
        }

        await AuthenticationClientsService.DeleteAuthenticationClientAsync(new DeleteAuthenticationClientRequest(Client.Id));
        NavigationManager.NavigateTo("/Admin/AuthenticationClients");
    }

    async Task SyncClaims()
    {
        var added = AccessClaims.Where(ac => OriginalClaims.All(oc => oc.UserId != ac.UserId || oc.Access.Value != ac.Access.Value)).ToList();
        var removed = OriginalClaims.Where(oc => AccessClaims.All(ac => ac.UserId != oc.UserId)).ToList();

        foreach(var add in added)
        {
            await UserAccessService.SetUserAccessAsync(new SetUserAccessClaimCommand(add.UserId, Client!.Id, add.Access.Value!));
        }

        foreach(var rem in removed)
        {
            await UserAccessService.RemoveUserAccessAsync(new RemoveUserAccessClaimCommand(rem.UserId, Client!.Id));
        }

        OriginalClaims = AccessClaims
            .Select(c => new AccessUsersMatrix.UserAccess(c.UserId, c.Access))
            .ToList();
    }
}
