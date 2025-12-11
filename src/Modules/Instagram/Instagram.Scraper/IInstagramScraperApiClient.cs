using Vitarish.Modules.Instagram.Scraper.Models;

namespace Vitarish.Instagram.Scraper;

public interface IInstagramScraperApiClient
{
    Task<DatasetItemsResponse> RunScraperAndGetDatasetItemsAsync(InputSchema input, CancellationToken ct = default);

    Task<RunInfoResponse> StartScraperRunAsync(InputSchema input, CancellationToken ct = default);

    Task<KeyValueStoreOutput> RunScraperSyncAsync(InputSchema input, CancellationToken ct = default);
}
