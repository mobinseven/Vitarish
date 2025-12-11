using Vitarish.Requests.Application.Queries;

namespace Vitarish.Accounts.Interface.Pages.Admin;

public partial class AdminRequestIdentitiesPage
{
    private readonly RequestIdentitiesFilterModel filters = new();
    private readonly List<Request> entries = [];

    private MudDataGrid<Request>? grid;
    private bool isLoading;
    private string? search;

    private bool HasActiveFilters
        => filters.HasValues || !string.IsNullOrWhiteSpace(search);

    private async Task<GridData<Request>> LoadEntriesAsync(GridState<Request> state)
    {
        isLoading = true;

        try
        {
            RequestIdentitySort[] sorts = state.SortDefinitions
                .Where(definition => !string.IsNullOrWhiteSpace(definition.SortBy))
                .Select(definition => new RequestIdentitySort(definition.SortBy!, definition.Descending))
                .ToArray();

            RequestIdentitiesFilter? filter = filters.HasValues
                ? filters.ToFilter()
                : null;

            GetRequestIdentitiesResult result = await RequestIdentitiesService.GetRequestIdentitiesAsync(
                state.Page,
                state.PageSize,
                search,
                filter,
                sorts.Length > 0 ? sorts : null);

            entries.Clear();
            entries.AddRange(result.Items);

            return new GridData<Request>
            {
                Items = entries,
                TotalItems = result.TotalItems
            };
        }
        finally
        {
            isLoading = false;
        }
    }

    private Task ReloadAsync() => grid is null ? Task.CompletedTask : grid.ReloadServerData();

    private async Task OnSearchChanged(string _) => await ReloadAsync();

    private async Task OnFilterChanged(string _) => await ReloadAsync();

    private async Task ClearFilters()
    {
        if (!HasActiveFilters)
        {
            return;
        }

        search = null;
        filters.Clear();
        await ReloadAsync();
    }

    private sealed class RequestIdentitiesFilterModel
    {
        public string? RequestId { get; set; }
        public string? Authority { get; set; }
        public string? ClientId { get; set; }
        public string? OwnerId { get; set; }
        public string? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? ResourceId { get; set; }
        public string? TraceId { get; set; }
        public string? AdditionalInfo { get; set; }

        public bool HasValues
            => !string.IsNullOrWhiteSpace(RequestId)
               || !string.IsNullOrWhiteSpace(Authority)
               || !string.IsNullOrWhiteSpace(ClientId)
               || !string.IsNullOrWhiteSpace(OwnerId)
               || !string.IsNullOrWhiteSpace(TenantId)
               || !string.IsNullOrWhiteSpace(UserId)
               || !string.IsNullOrWhiteSpace(ResourceId)
               || !string.IsNullOrWhiteSpace(TraceId)
               || !string.IsNullOrWhiteSpace(AdditionalInfo);

        public RequestIdentitiesFilter ToFilter() => new(
            RequestId,
            Authority,
            ClientId,
            OwnerId,
            TenantId,
            UserId,
            ResourceId,
            TraceId,
            AdditionalInfo);

        public void Clear()
        {
            RequestId = null;
            Authority = null;
            ClientId = null;
            OwnerId = null;
            TenantId = null;
            UserId = null;
            ResourceId = null;
            TraceId = null;
            AdditionalInfo = null;
        }
    }
}
