namespace Vitarish.Interfaces.Base.Common;

public sealed class StackPaginationState
{
    public int Page { get; private set; } = 1;
    public int TotalItems { get; private set; }
    public int PageSize { get; private set; } = 10;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize)));
    public int Skip => (Page - 1) * Math.Max(1, PageSize);

    public event Action? StateChanged;

    public void Configure(int totalItems, int pageSize)
    {
        TotalItems = Math.Max(0, totalItems);
        PageSize = Math.Max(1, pageSize);

        if(Page > TotalPages)
            Page = TotalPages;

        NotifyStateChanged();
    }

    public void SetPage(int page)
    {
        var boundedPage = Math.Clamp(page, 1, TotalPages);
        if(boundedPage == Page)
            return;

        Page = boundedPage;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
