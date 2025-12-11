using Vitarish.Modules.Instagram.Scraper.Models;

namespace Vitarish.Instagram.Scraper;

public sealed record RunInfoResponse(RunsResponseSchema Run)
{
    public string? RunId => Run?.Data?.Id;
    public string? DefaultDatasetId => Run?.Data?.DefaultDatasetId;
}
