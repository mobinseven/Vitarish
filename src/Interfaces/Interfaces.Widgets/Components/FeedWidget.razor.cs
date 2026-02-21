using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class FeedWidget
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter] public string Title { get; set; } = "Activity Feed";
    [Parameter] public int PageSize { get; set; } = 20;
    [Parameter] public int MaxItems { get; set; } = 100;
    [Parameter] public bool ShowRefreshButton { get; set; } = true;
    [Parameter] public IReadOnlyList<string>? SourceIds { get; set; }
    [Parameter] public EventCallback<FeedItem> OnItemClicked { get; set; }

    private readonly List<FeedItem> _feedItems = [];
    private bool _isLoading;
    private string? _error;
    private int _totalItemsLoaded;

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async ValueTask<ItemsProviderResult<FeedItem>> LoadFeedItemsAsync(
        ItemsProviderRequest request)
    {
        if (_isLoading)
        {
            return new ItemsProviderResult<FeedItem>([], 0);
        }

        try
        {
            _isLoading = true;
            _error = null;

            // Calculate how many items we need to fetch
            int skip = request.StartIndex;
            int take = Math.Min(request.Count, PageSize);

            // If we already have these items cached, return them
            if (skip < _feedItems.Count)
            {
                var cachedItems = _feedItems
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                if (cachedItems.Count >= take || _totalItemsLoaded >= MaxItems)
                {
                    return new ItemsProviderResult<FeedItem>(cachedItems, _feedItems.Count);
                }
            }

            // Fetch new items
            var options = new FeedQueryOptions
            {
                Limit = take,
                Skip = skip,
                SourceIds = SourceIds
            };

            var query = new GetFeedQuery(options);
            var items = await Mediator.Send(query);

            // Update cache
            foreach (var item in items)
            {
                if (!_feedItems.Any(i => i.Id == item.Id))
                {
                    _feedItems.Add(item);
                }
            }

            _totalItemsLoaded = _feedItems.Count;

            return new ItemsProviderResult<FeedItem>(items, _feedItems.Count);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load feed: {ex.Message}";
            return new ItemsProviderResult<FeedItem>([], 0);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        _feedItems.Clear();
        _totalItemsLoaded = 0;
        _error = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnItemClick(FeedItem item)
    {
        if (OnItemClicked.HasDelegate)
        {
            await OnItemClicked.InvokeAsync(item);
        }
    }
}
