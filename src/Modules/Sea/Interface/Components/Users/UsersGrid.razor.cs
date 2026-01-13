using Microsoft.AspNetCore.Components.Routing;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Users;

public partial class UsersGrid : UsersBase, IDisposable
{
    private string? SearchString;
    private IList<UserDto> users = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadData();
        }
    }

    private async Task ReloadData()
    {
        IList<User> usersInRole = await UsersService.GetUsersInRoleAsync("User", CancellationToken.None);
        users = [.. usersInRole.Select(u => new UserDto(u))];
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
        UpdateSearchFromUri(NavigationManager.Uri);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            if (UpdateSearchFromUri(e.Location))
            {
                await ReloadData();
            }
        }
        catch
        {
            // Ignore navigation refresh errors.
        }
    }

    private bool UpdateSearchFromUri(string uri)
    {
        string? query = SearchQueryHelper.ExtractQuery(NavigationManager, uri);
        if (string.Equals(SearchString, query, StringComparison.Ordinal))
        {
            return false;
        }

        SearchString = query;
        return true;
    }

    void IDisposable.Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
