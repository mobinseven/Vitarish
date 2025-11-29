namespace Vorn.Aaas.Client.Admin;

using Microsoft.AspNetCore.Components;
using Vorn.Aaas.Client.Models;
using Vorn.Aaas.Client.Services;

public partial class AccessUsersMatrix : ComponentBase
{
    [Parameter] public string ClientId { get; set; } = string.Empty;

    [Inject] IAccessService AccessService { get; set; } = default!;

    bool initialized;

    protected override async Task OnInitializedAsync()
    {
        await AccessService.InitializeAsync(ClientId);
        initialized = true;
    }

    IEnumerable<string> Roles => AccessService.Roles;

    IReadOnlyList<UserDto> Users => AccessService.Users;

    async Task Remove(string userId)
    {
        await AccessService.RemoveUserAccessAsync(userId, ClientId);
        UserDto? user = Users.FirstOrDefault(u => u.Id == userId);
        if (user is not null)
        {
            user.Access.Read = null;
            user.Access.Write = null;
            user.Access.Admin = false;
        }
    }
}
