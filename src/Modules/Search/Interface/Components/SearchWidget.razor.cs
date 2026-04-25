using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Interface.Components;

public partial class SearchWidget : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    /// <summary>
    /// The logical index name to search (e.g. "sea.projects").
    /// </summary>
    [Parameter, EditorRequired]
    public string IndexName { get; set; } = string.Empty;

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

    private async Task HandleKeyUpAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            _result = null;
            return;
        }

        _loading = true;

        try
        {
            SearchOptions options = Options with { Type = Type ?? Options.Type };

            _result = await Mediator.Send(new SearchQuery(IndexName, _query, options));
        }
        catch
        {
            _result = new SearchResult { Items = [], TotalCount = 0, Query = _query };
        }
        finally
        {
            _loading = false;
        }
    }

    private void Navigate(SearchDocument item)
    {
        if (!string.IsNullOrWhiteSpace(item.NavigationUrl))
        {
            NavigationManager.NavigateTo(item.NavigationUrl);
        }
    }
}
