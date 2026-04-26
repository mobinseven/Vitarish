using Microsoft.Extensions.Localization;
using Vitarish.Apps.Base;

namespace Vitarish.Apps.Aggregator;

public record AppRecord(string Id, string Name);

public class AppsProvider(IStringLocalizer localizer) : IAppsProvider
{
    public IReadOnlyList<AppRecord> GetAppRecords()
    {
        List<IApp> apps = AppsExtensions.GetApps();
        return [.. apps.Select(e => new AppRecord(e.Name, localizer[e.Name]))];
    }
}

public interface IAppsProvider
{
    IReadOnlyList<AppRecord> GetAppRecords();
}
