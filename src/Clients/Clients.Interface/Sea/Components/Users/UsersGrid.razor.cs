namespace Vitarish.Clients.Interface.Sea.Components.Users;

public partial class UsersGrid : UsersBase, IDisposable
{
    private MudDataGrid<UserDto>? grid;
    private string? SearchString;
    private async Task<GridData<UserDto>> ReloadData(GridState<UserDto> gridState)
    {
        IList<UserDto> users = [.. AccessService.Users];
        return new GridData<UserDto>
        {
            Items = users,
            TotalItems = users.Count
        };
    }
    protected override void OnInitialized() => SearchService.OnSearch += OnSearch;

    protected async void OnSearch(object? sender, string? query)
    {
        SearchString = query;
        await grid!.ReloadServerData();
    }

    void IDisposable.Dispose()
    {
        SearchService.OnSearch -= OnSearch;
        GC.SuppressFinalize(this);
    }
}
