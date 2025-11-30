namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

using System.Collections.Generic;
using System.Linq;
using Vorn.Aaas.RequestIdentities.Application.Queries;

public partial class AdminRequestIdentitiesPage
{
    readonly RequestIdentitiesFilterModel filters = new();
    readonly List<RequestIdentity> entries = [];

    MudDataGrid<RequestIdentity>? grid;
    bool isLoading;
    string? search;

    bool HasActiveFilters
        => filters.HasValues || !string.IsNullOrWhiteSpace(search);

    async Task<GridData<RequestIdentity>> LoadEntriesAsync(GridState<RequestIdentity> state)
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

            return new GridData<RequestIdentity>
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

    Task ReloadAsync()
    {
        if(grid is null)
        {
            return Task.CompletedTask;
        }

        return grid.ReloadServerData();
    }

    async Task OnSearchChanged(string _)
    {
        await ReloadAsync();
    }

    async Task OnFilterChanged(string _)
    {
        await ReloadAsync();
    }

    async Task ClearFilters()
    {
        if(!HasActiveFilters)
        {
            return;
        }

        search = null;
        filters.Clear();
        await ReloadAsync();
    }

    sealed class RequestIdentitiesFilterModel
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
