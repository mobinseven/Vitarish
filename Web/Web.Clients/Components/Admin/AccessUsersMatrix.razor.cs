using Microsoft.AspNetCore.Components;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Admin;

/// <summary>
/// Component for managing user access.
/// </summary>
public partial class AccessUsersMatrix : ComponentBase
{
    /// <summary>
    /// The client ID to manage users for.
    /// </summary>
    [Parameter]
    public string ClientId { get; set; } = string.Empty;

    [Inject]
    private IAccessService AccessService { get; set; } = default!;

    private bool initialized;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await AccessService.InitializeAsync(ClientId);
        initialized = true;
    }

    private IEnumerable<string> Roles => AccessService.Roles;

    private IReadOnlyList<UserDto> Users => AccessService.Users;

    private async Task Remove(string userId)
    {
        await AccessService.RemoveUserAccessAsync(userId, ClientId);
        UserDto? user = Users.FirstOrDefault(u => u.Id == userId);
        if (user is not null)
        {
            user.Access!.Read = null;
            user.Access.Write = null;
            user.Access.Admin = false;
        }
    }
}
