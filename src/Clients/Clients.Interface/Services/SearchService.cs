namespace Vitarish.Clients.Interface.Services;

[RegisterScoped]
public sealed class SearchService : ISearchService
{
    private readonly ILayoutService layoutService;
    private event SearchHandler? searchRequested;

    public SearchService(ILayoutService layoutService)
    {
        this.layoutService = layoutService;
    }

    public string? LastSearch { get; private set; } = string.Empty;

    public event SearchHandler OnSearch
    {
        add
        {
            bool wasEnabled = IsEnabled;
            searchRequested += value;

            if (IsEnabled != wasEnabled)
            {
                layoutService.NotifySearchStateChanged(this);
            }
        }
        remove
        {
            bool wasEnabled = IsEnabled;
            searchRequested -= value;

            if (IsEnabled != wasEnabled)
            {
                layoutService.NotifySearchStateChanged(this);
            }
        }
    }

    public bool IsEnabled => searchRequested is not null;

    public void Search(object? sender, string? query)
    {
        string? previous = LastSearch;
        LastSearch = query;

        if (!string.Equals(previous, query, StringComparison.Ordinal))
        {
            layoutService.NotifySearchStateChanged(this);
        }

        SearchHandler? handler = searchRequested;

        if (string.IsNullOrWhiteSpace(query))
        {
            handler?.Invoke(sender, null);
        }
        else
        {
            handler?.Invoke(sender, query);
        }
    }
}

public delegate void SearchHandler(object? sender, string? query);

public interface ISearchService
{
    bool IsEnabled { get; }
    string? LastSearch { get; }

    event SearchHandler OnSearch;

    void Search(object? sender, string? query);
}
