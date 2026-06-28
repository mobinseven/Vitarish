using Vitarish.Clinic.App;
using Vitarish.Apps.Base.Core;
using Microsoft.Extensions.Localization;

namespace Vitarish.Apps.Aggregator.Core;

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
