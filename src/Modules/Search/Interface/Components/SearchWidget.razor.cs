using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Interface.Components;

public partial class SearchWidget : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    /// <summary>
    /// Optional logical index name to search (e.g. "sea.projects").
    /// When null, the widget performs a global search across every registered index.
    /// </summary>
    [Parameter]
    public string? IndexName { get; set; }

    /// <summary>
    /// Optional document type filter passed to <see cref="SearchOptions.Type"/>.
    /// </summary>
    [Parameter]
    public string? Type { get; set; }

    /// <summary>
    /// Pagination and filtering options. Defaults are applied when not provided.
    /// </summary>
    [Parameter]
    public SearchOptions Options { get; set; } = new();

    private string _query = string.Empty;
    private SearchResult? _result;
    private bool _loading;
    private string? _errorMessage;
    private int _pageIndex;
    private IReadOnlyList<SearchResultGroup> _resultGroups = [];

    private int PageSize => SearchOptions.NormalizeLimit(Options.Limit);

    private long TotalPages => _result is null || _result.TotalCount == 0
        ? 0
        : (long)Math.Ceiling((double)_result.TotalCount / PageSize);

    private bool HasPreviousPage => _pageIndex > 0 && !_loading;

    private bool HasNextPage => _result is not null && _pageIndex + 1 < TotalPages && !_loading;

    private async Task HandleKeyUpAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        _pageIndex = 0;
        await SearchPageAsync();
    }

    private async Task SearchPageAsync()
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            _result = null;
            _resultGroups = [];
            _errorMessage = null;
            _pageIndex = 0;
            return;
        }

        _loading = true;
        _errorMessage = null;

        try
        {
            SearchOptions options = Options with
            {
                Skip = _pageIndex * PageSize,
                Limit = PageSize,
                Type = Type ?? Options.Type
            };

            _result = await Mediator.Send(new SearchQuery(IndexName, _query, options));
            _resultGroups = CreateResultGroups(_result.Items);
        }
        catch
        {
            _result = null;
            _resultGroups = [];
            _errorMessage = Localizer["SearchFailed"];
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (!HasPreviousPage)
        {
            return;
        }

        _pageIndex--;
        await SearchPageAsync();
    }

    private async Task GoToNextPageAsync()
    {
        if (!HasNextPage)
        {
            return;
        }

        _pageIndex++;
        await SearchPageAsync();
    }

    private IReadOnlyList<SearchResultGroup> CreateResultGroups(IReadOnlyList<SearchDocument> items) =>
        [.. items
            .GroupBy(GetIndexDisplayName)
            .Select(group => new SearchResultGroup(group.Key, [.. group]))];

    private string GetIndexDisplayName(SearchDocument item) =>
        string.IsNullOrWhiteSpace(item.IndexName)
            ? Localizer["UnknownIndex"]
            : item.IndexName;

    private string GetPageSummary()
    {
        if (_result is null || _result.TotalCount == 0)
        {
            return string.Empty;
        }

        long firstItem = _pageIndex * PageSize + 1;
        long lastItem = Math.Min((_pageIndex + 1) * PageSize, _result.TotalCount);

        return string.Format(Localizer["SearchPageSummary"], firstItem, lastItem, _result.TotalCount);
    }

    private void Navigate(SearchDocument item)
    {
        if (!string.IsNullOrWhiteSpace(item.NavigationUrl))
        {
            NavigationManager.NavigateTo(item.NavigationUrl);
        }
    }

    private sealed record SearchResultGroup(string IndexName, IReadOnlyList<SearchDocument> Items);
}
