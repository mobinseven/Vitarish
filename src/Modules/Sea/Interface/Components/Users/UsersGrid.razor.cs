namespace Vitarish.Sea.Interface.Components.Users;

public partial class UsersGrid : UsersBase, IDisposable
{
    private UserSummaryResponse[] users = [];
    protected override Task ReloadRequired()
    {
        users = [.. GetUsers()];
        return Task.CompletedTask;
    }
}
