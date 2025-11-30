namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

using Vorn.Aaas.Users.Application.Commands;
using ClientClaimDto = Client.Contracts.ClaimDto;

public partial class AdminUserPage
{
    [Parameter] public string? UserId { get; set; }
    string password = string.Empty;
    [Inject] IPasswordHasher<User> PasswordHasher { get; set; } = default!;
    User? User;
    List<ClientClaimDto>? UserAccessClaims { get; set; }
    List<ClientClaimDto>? UserOtherClaims { get; set; }
    protected override async Task OnParametersSetAsync()
    {
        if(UserId is not null)
        {
            User = await UserService.GetUserAsync(UserId);
            var UserClaims = (await UserService.GetUserClaimsAsync(UserId)).Select(c => new ClientClaimDto { Type = c.Type, Value = c.Value }).ToList();
            UserAccessClaims = UserClaims.Where(c => c.Type == AaasConstants.AccessClaim).ToList();
            UserOtherClaims = UserClaims.Where(c => c.Type != AaasConstants.AccessClaim).ToList();
        }
        else
        {
            Back();
        }
    }
    async void Delete()
    {
        if(User is null)
        {
            return;
        }

        IdentityResult res = await UserService.DeleteUserAsync(new DeleteUserCommand(User.Id));
        if(res.Succeeded)
        {
            Snackbar.Add(Localizer["Deleted"], Severity.Success);
        }
        else
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
        }
    }
    async void Save()
    {
        if(User is null)
        {
            return;
        }

        if(!string.IsNullOrWhiteSpace(password))
        {
            User.PasswordHash = PasswordHasher.HashPassword(User, password);
        }

        var userRes = await UserService.UpdateUserAsync(new UpdateUserCommand(User));
        if(!userRes.Succeeded)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
            return;
        }

        if(UserOtherClaims is null || UserAccessClaims is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
            return;
        }

        var servRes = await UserService.UpdateUserClaimsAsync(new UpdateUserClaimsCommand(User, UserOtherClaims.Union(UserAccessClaims)!.Select(c => new Claim(c.Type, c.Value)).ToArray()));
        if(!servRes.Succeeded)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
        }

        Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    void Back() => NavigationManager.NavigateTo("/Admin/Users");

}
