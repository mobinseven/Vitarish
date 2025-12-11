using Vitarish.Modules.Instagram.Scraper.Models;

namespace Vitarish.Instagram.Scraper;

public sealed record InstagramRunInfo(string? RunId, string? DefaultDatasetId, RunsResponseSchema Raw);
