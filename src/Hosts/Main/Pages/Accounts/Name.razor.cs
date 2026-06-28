using Vitarish.Users.Contracts.Requests;

namespace Vitarish.Main.Pages.Accounts;

public partial class Name
{
    private async Task SaveNameAsync()
    {
        if (User?.Id is null) return;
        HttpClient client = HttpClientFactory.CreateClient();
        _ = await client.PutAsJsonAsync($"/api/modules/users/{User.Id}/profile", new UpdateUserProfileRequest(User.FirstName, User.LastName));
    }
}
