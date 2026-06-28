using Vitarish.Users.Contracts.Requests;

namespace Vitarish.Main.Pages.Accounts;

public partial class Theme
{
    private async Task SetThemeAsync(string theme)
    {
        if (User?.Id is null) return;
        HttpClient client = HttpClientFactory.CreateClient();
        _ = await client.PutAsJsonAsync($"/api/modules/users/{User.Id}/theme", new SetUserThemeRequest(theme));
    }
}
