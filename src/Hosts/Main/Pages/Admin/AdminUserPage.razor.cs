using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure;
namespace Vitarish.Main.Pages.Admin;

public partial class AdminUserPage
{
    [Parameter] public string? UserId { get; set; }
    private string password = string.Empty;
    [Inject] private IPasswordHasher<User> PasswordHasher { get; set; } = default!;
    private User? User;
    private List<ClaimDto>? UserAccessClaims { get; set; }
    private List<ClaimDto>? UserOtherClaims { get; set; }
    protected override async Task OnParametersSetAsync()
    {
        if (UserId is not null)
        {
            User = await UsersService.GetUserAsync(UserId);
            var UserClaims = (await UsersService.GetUserClaimsAsync(UserId)).Select(c => new ClaimDto { Type = c.Type, Value = c.Value }).ToList();
            UserAccessClaims = UserClaims.Where(c => c.Type == IdentityAccessConstants.AccessClaim).ToList();
            UserOtherClaims = UserClaims.Where(c => c.Type != IdentityAccessConstants.AccessClaim).ToList();
        }
        else
        {
            Back();
        }
    }
    private async void Delete()
    {
        if (User is null)
        {
            return;
        }

        IdentityResult res = await UsersService.DeleteUserAsync(new DeleteUserCommand(User.Id));
        if (res.Succeeded)
        {
            Snackbar.Add(Localizer["Deleted"], Severity.Success);
        }
        else
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
        }
    }
    private async void Save()
    {
        if (User is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            User.PasswordHash = PasswordHasher.HashPassword(User, password);
        }

        var userRes = await UsersService.UpdateUserAsync(new UpdateUserCommand(User));
        if (!userRes.Succeeded)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
            return;
        }

        if (UserOtherClaims is null || UserAccessClaims is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
            return;
        }

        var servRes = await UsersService.UpdateUserClaimsAsync(new UpdateUserClaimsCommand(User, UserOtherClaims.Union(UserAccessClaims)!.Select(c => new Claim(c.Type, c.Value)).ToArray()));
        if (!servRes.Succeeded)
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
        }

        Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    private void Back() => NavigationManager.NavigateTo("/Admin/Users");

}
