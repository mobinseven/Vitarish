using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Agents;

public sealed record ComposedAgentManifest<TContext>(
    AgentDefinition Definition,
    AgentExecutionLimits Limits,
    IReadOnlyList<IAgentTool> Tools,
    IReadOnlyDictionary<string, string> ToolCapabilities,
    IReadOnlyList<string> ActionTypes,
    IReadOnlyList<AgentProfileLayer> ProfileLayers,
    IReadOnlyDictionary<string, int> CapabilityRevisions,
    IReadOnlyDictionary<string, string> ExposureRevisions,
    string CompatibilityKey);

public sealed class AgentProfileComposer<TContext>(
    AgentCapabilityCatalog<TContext> catalog,
    IntelligenceAgentToolCatalog? intelligenceToolCatalog = null)
{
    public ComposedAgentManifest<TContext> Compose(
        AgentProfile<TContext> profile,
        TContext context,
        Func<string, AgentExposureDecision> exposure)
    {
        ResolvedAgentProfile<TContext> resolved = profile.Resolve();
        List<IAgentTool> tools = [];
        Dictionary<string, string> toolCapabilities = new(StringComparer.Ordinal);
        List<string> actions = [];
        Dictionary<string, int> capabilityRevisions = new(StringComparer.Ordinal);
        Dictionary<string, string> exposureRevisions = new(StringComparer.Ordinal);
        List<InstructionSection> instructions = [.. resolved.Instructions];

        foreach (string key in resolved.CapabilityKeys)
        {
            if (!catalog.TryGet(key, out IAgentCapabilityPack<TContext>? pack) || pack is null)
                throw new InvalidOperationException($"Capability '{key}' is not registered.");
            capabilityRevisions[key] = pack.BehaviorRevision;
            AgentExposureDecision decision = exposure(key) ?? throw new InvalidOperationException($"Exposure decision for '{key}' is required.");
            exposureRevisions[key] = decision.Revision;
            if (!decision.Enabled) continue;
            AgentCapabilityContribution contribution = pack.CreateContribution(context);
            foreach (InstructionSection instruction in contribution.Instructions)
            {
                if (instructions.Any(x => x.Key == instruction.Key)) throw new InvalidOperationException($"Instruction '{instruction.Key}' was duplicated.");
                instructions.Add(instruction);
            }
            foreach (IAgentTool tool in contribution.Tools)
            {
                if (tools.Any(x => x.Definition.Name == tool.Definition.Name)) throw new InvalidOperationException($"Tool '{tool.Definition.Name}' was duplicated.");
                tools.Add(tool);
                toolCapabilities.Add(tool.Definition.Name, key);
            }
            foreach (string action in contribution.ActionTypes)
            {
                if (actions.Contains(action, StringComparer.Ordinal)) throw new InvalidOperationException($"Action '{action}' was duplicated.");
                actions.Add(action);
            }
        }

        IEnumerable<IAgentTool> genericTools = intelligenceToolCatalog?.Tools.Cast<IAgentTool>() ?? [];
        foreach (IAgentTool tool in genericTools)
        {
            if (tools.Any(existing => existing.Definition.Name == tool.Definition.Name))
                throw new InvalidOperationException($"Tool '{tool.Definition.Name}' was duplicated.");

            tools.Add(tool);
            toolCapabilities.Add(tool.Definition.Name, IntelligenceAgentToolCatalog.CapabilityKey);
        }

        string flattenedInstructions = string.Join("\n\n", instructions.OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Text));
        AgentDefinition definition = new(resolved.Model, flattenedInstructions, resolved.Result, resolved.ReasoningLevel, resolved.DisplayName);
        string compatibilityKey = AgentManifestCompatibility.Calculate(resolved, definition, tools, capabilityRevisions, exposureRevisions, instructions);
        return new(definition, resolved.Limits, tools, toolCapabilities, actions, resolved.Layers, capabilityRevisions, exposureRevisions, compatibilityKey);
    }
}

public static class AgentManifestCompatibility
{
    public static string Calculate<TContext>(ResolvedAgentProfile<TContext> profile, AgentDefinition definition,
        IEnumerable<IAgentTool> tools, IReadOnlyDictionary<string, int> capabilityRevisions,
        IReadOnlyDictionary<string, string> exposureRevisions,
        IEnumerable<InstructionSection>? effectiveInstructions = null)
    {
        JsonObject manifest = new()
        {
            ["agentKey"] = profile.AgentKey, ["displayName"] = profile.DisplayName, ["model"] = profile.Model,
            ["reasoningLevel"] = definition.ReasoningLevel.ToString(),
            ["limits"] = JsonSerializer.SerializeToNode(profile.Limits),
            ["result"] = profile.Result is null ? null : new JsonObject
            {
                ["name"] = profile.Result.Name,
                ["strict"] = profile.Result.Strict,
                ["schema"] = CanonicalSchema(profile.Result.JsonSchema)
            },
            ["instructions"] = string.Join("\n\n", (effectiveInstructions ?? profile.Instructions).OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.CompatibilityText ?? x.Text)),
            ["layers"] = new JsonArray(profile.Layers.Select(x => (JsonNode?)new JsonObject { ["key"] = x.Key, ["revision"] = x.Revision }).ToArray()),
            ["compatibilityMaterials"] = new JsonObject(profile.CompatibilityMaterials.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => (JsonNode?)x.Value, StringComparer.Ordinal)),
            ["capabilities"] = new JsonArray(capabilityRevisions.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => (JsonNode?)new JsonObject { ["key"] = x.Key, ["revision"] = x.Value, ["exposure"] = exposureRevisions[x.Key] }).ToArray()),
            ["tools"] = new JsonArray(tools.OrderBy(x => x.Definition.Name, StringComparer.Ordinal).Select(x => (JsonNode?)new JsonObject { ["name"] = x.Definition.Name, ["description"] = x.Definition.Description, ["schema"] = CanonicalSchema(x.Definition.ParametersJsonSchema) }).ToArray())
        };
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = false })));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static JsonNode CanonicalSchema(string json)
    {
        JsonNode? node = JsonNode.Parse(json) ?? throw new InvalidOperationException("JSON schema cannot be empty.");
        return Sort(node);
    }

    private static JsonNode Sort(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value is null ? null : Sort(x.Value), StringComparer.Ordinal)),
        JsonArray array => new JsonArray(array.Select(x => x is null ? null : Sort(x)).ToArray()),
        _ => node.DeepClone()
    };
}
