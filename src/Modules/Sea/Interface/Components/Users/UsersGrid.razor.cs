using Microsoft.AspNetCore.Components.Routing;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Users;

public partial class UsersGrid : UsersBase, IDisposable
{
    private MudDataGrid<UserDto>? grid;
    private string? SearchString;

    private async Task<GridData<UserDto>> ReloadData(GridState<UserDto> gridState)
    {
        IList<User> usersInRole = await UsersService.GetUsersInRoleAsync("User", CancellationToken.None);
        IList<UserDto> users = [.. usersInRole.Select(u => new UserDto(u))];
        return new GridData<UserDto>
        {
            Items = users,
            TotalItems = users.Count
        };
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
        UpdateSearchFromUri(NavigationManager.Uri);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _ = HandleLocationChangedAsync(e);

    private Task HandleLocationChangedAsync(LocationChangedEventArgs e)
    {
        UpdateSearchFromUri(e.Location);
        if (grid is not null)
        {
            return grid.ReloadServerData();
        }

        return Task.CompletedTask;
    }

    private void UpdateSearchFromUri(string uri)
    {
        SearchString = SearchQueryHelper.ExtractQuery(NavigationManager, uri);
    }

    void IDisposable.Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
