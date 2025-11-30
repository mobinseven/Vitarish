
using System.Diagnostics.CodeAnalysis;
using Vorn.Aaas.Client.Api.Contracts;

namespace Vorn.Aaas.Client.Api.Options;

public sealed class SignalRClientOptions
{
    private readonly Dictionary<string, SignalRClientRegistration> clients = new(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, SignalRClientRegistration> Clients => clients;

    internal static string KeyFromType(Type type) => type.FullName ?? type.Name;

    internal bool TryGetRegistration(string key, [NotNullWhen(true)] out SignalRClientRegistration? registration) => clients.TryGetValue(key, out registration);
}
