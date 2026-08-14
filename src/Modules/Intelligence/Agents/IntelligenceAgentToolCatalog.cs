using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Agents;

public sealed class IntelligenceAgentToolCatalog
{
    public const string CapabilityKey = "Intelligence";

    public IntelligenceAgentToolCatalog(IEnumerable<IUniversalIntelligenceAgentTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        List<IUniversalIntelligenceAgentTool> validated = [];
        foreach (IUniversalIntelligenceAgentTool tool in tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            if (string.IsNullOrWhiteSpace(tool.Definition.Name))
                throw new InvalidOperationException("An Intelligence agent tool must declare a non-empty name.");

            validated.Add(tool);
        }

        Tools = validated
            .GroupBy(tool => tool.Definition.Name, StringComparer.Ordinal)
            .Select(group => group.Count() == 1
                ? group.Single()
                : throw new InvalidOperationException($"Intelligence agent tool '{group.Key}' was registered more than once."))
            .OrderBy(tool => tool.Definition.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<IUniversalIntelligenceAgentTool> Tools { get; }
}
