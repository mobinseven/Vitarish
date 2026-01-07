using Microsoft.AspNetCore.Components.Routing;
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

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateSearchFromUri(e.Location);
        if (grid is not null)
        {
            await grid.ReloadServerData();
        }
    }

    private void UpdateSearchFromUri(string uri)
    {
        SearchString = ExtractQuery(uri);
    }

    private string? ExtractQuery(string uri)
    {
        Uri absolute = NavigationManager.ToAbsoluteUri(uri);
        string query = absolute.Query;
        if (string.IsNullOrEmpty(query) || query.Length <= 1)
        {
            return null;
        }

        foreach (string segment in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            if (!string.Equals(segment[..separatorIndex], "q", StringComparison.Ordinal))
            {
                continue;
            }

            string value = segment[(separatorIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }

        return null;
    }

    void IDisposable.Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
