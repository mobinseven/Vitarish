using Microsoft.Extensions.Localization;

namespace Modules.Base.Interface.Tests.TestInfrastructure;

public sealed class TrackingStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly Dictionary<string, IStringLocalizer> _localizers = new(StringComparer.Ordinal);

    public List<string> Created { get; } = [];

    public void Register(Type type, IStringLocalizer localizer)
        => _localizers[type.FullName ?? type.Name] = localizer;

    public void Register(string baseName, IStringLocalizer localizer)
        => _localizers[baseName] = localizer;

    public IStringLocalizer Create(Type resourceSource)
    {
        string key = resourceSource.FullName ?? resourceSource.Name;
        Created.Add(key);
        return _localizers.TryGetValue(key, out IStringLocalizer? localizer)
            ? localizer
            : new TestStringLocalizer(new Dictionary<string, string>());
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        Created.Add(baseName);
        return _localizers.TryGetValue(baseName, out IStringLocalizer? localizer)
            ? localizer
            : new TestStringLocalizer(new Dictionary<string, string>());
    }
}
