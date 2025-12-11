namespace Vitarish.Instagram.Scraper;

public interface IInstagramScraperService
{
    Task<InstagramData[]> FetchAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default);

    Task<InstagramRunInfo> StartRunAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default);

    Task<InstagramRawOutput> RunSyncAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default);
}
