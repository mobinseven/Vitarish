
using Microsoft.AspNetCore.Components;
using Vorn.Aaas.Client.Abstractions;
using Vorn.Aaas.Client.Contracts;

namespace Vorn.Aaas.Client.Admin;

public partial class AccessUsersMatrix : ComponentBase
{
    [Parameter] public string ClientId { get; set; } = string.Empty;

    [Inject] private IAccessService AccessService { get; set; } = default!;

    private bool initialized;

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
            user.Access.Read = null;
            user.Access.Write = null;
            user.Access.Admin = false;
        }
    }
}
