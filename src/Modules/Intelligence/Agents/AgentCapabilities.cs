using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Agents;

public sealed record AgentExposureDecision(bool Enabled, string Revision)
{
    public static AgentExposureDecision Denied(string revision) => new(false, revision);
}

public sealed record AgentCapabilityContribution(
    IReadOnlyList<InstructionSection> Instructions,
    IReadOnlyList<IAgentTool> Tools,
    IReadOnlyList<string> ActionTypes,
    IReadOnlyDictionary<string, string>? Diagnostics = null);

public interface IAgentCapabilityPack<TContext>
{
    string Key { get; }
    int BehaviorRevision { get; }
    AgentCapabilityContribution CreateContribution(TContext context);
}

public sealed class AgentCapabilityCatalog<TContext>
{
    private readonly IReadOnlyDictionary<string, IAgentCapabilityPack<TContext>> _packs;

    public AgentCapabilityCatalog(IEnumerable<IAgentCapabilityPack<TContext>> packs)
    {
        _packs = packs.GroupBy(x => x.Key, StringComparer.Ordinal).ToDictionary(
            group => group.Key,
            group => group.Count() == 1 ? group.Single() : throw new InvalidOperationException($"Capability '{group.Key}' was registered more than once."),
            StringComparer.Ordinal);
    }

    public bool TryGet(string key, out IAgentCapabilityPack<TContext>? pack) => _packs.TryGetValue(key, out pack);
}
