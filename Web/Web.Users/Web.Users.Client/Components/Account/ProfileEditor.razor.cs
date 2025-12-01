using Vorn.Aaas.Users.Application.Commands;

namespace Vitarish.Web.Users.Client.Components.Account;

public partial class ProfileEditor
{

    [Parameter] public string Username { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

    private User User = default!;

    protected override async Task OnInitializedAsync() => User = (await UserService.GetUserByNameAsync(Username))!;
    private async void UpdateUser()
    {
        await UserService.UpdateUserAsync(new UpdateUserCommand(User));
        if(ReturnUrl is null)
        {
            Snackbar.Add(Localizer["ProfileUpdated"]);
        }
        else
        {
            RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
        }
    }

}
