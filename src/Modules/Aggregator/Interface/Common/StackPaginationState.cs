namespace Vitarish.Modules.Aggregator.Interface.Common;

public sealed class StackPaginationState
{
    public int Page { get; private set; } = 1;
    public int TotalItems { get; private set; }
    public int PageSize { get; private set; } = 10;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public int Skip => (Page - 1) * PageSize;

    public event Action? StateChanged;

    public void Configure(int totalItems, int pageSize)
    {
        int newTotalItems = Math.Max(0, totalItems);
        int newPageSize = Math.Max(1, pageSize);

        if (TotalItems == newTotalItems && PageSize == newPageSize)
        {
            return;
        }

        TotalItems = newTotalItems;
        PageSize = newPageSize;

        if (Page > TotalPages)
        {
            Page = TotalPages;
        }

        NotifyStateChanged();
    }

    public void SetPage(int page)
    {
        SetPageCore(page, notify: true);
    }

    public void SetPageWithoutNotification(int page)
    {
        SetPageCore(page, notify: false);
    }

    private void SetPageCore(int page, bool notify)
    {
        int boundedPage = Math.Clamp(page, 1, TotalPages);
        if (boundedPage == Page)
        {
            return;
        }

        Page = boundedPage;

        if (notify)
        {
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
