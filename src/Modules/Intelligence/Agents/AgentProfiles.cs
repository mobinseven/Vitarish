using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Agents;

public abstract class AgentProfile<TContext>
{
    protected abstract void Configure(AgentProfileBuilder<TContext> builder);

    public ResolvedAgentProfile<TContext> Resolve()
    {
        AgentProfileBuilder<TContext> builder = new();
        Configure(builder);
        return builder.Build();
    }
}

public sealed class AgentProfileBuilder<TContext>
{
    private readonly List<AgentProfileLayer> _layers = [];
    private readonly Dictionary<string, InstructionSection> _instructions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _compatibilityMaterials = new(StringComparer.Ordinal);
    private readonly List<string> _capabilities = [];
    private string? _agentKey;
    private string? _displayName;
    private string? _model;
    private AgentReasoningLevel _reasoningLevel = AgentReasoningLevel.Low;
    private AgentResultDefinition? _result;
    private AgentExecutionLimits _limits = new();

    public void ApplyLayer(string layerKey, int revision, Action<AgentProfileBuilder<TContext>> configure)
    {
        if (string.IsNullOrWhiteSpace(layerKey) || revision < 1 || configure is null)
            throw new ArgumentException("Profile layers require a key, positive revision, and configuration.");
        if (_layers.Any(layer => layer.Key == layerKey))
            throw new InvalidOperationException($"Profile layer '{layerKey}' was registered more than once.");
        _layers.Add(new(layerKey, revision));
        configure(this);
    }

    public void SetIdentity(string agentKey, string displayName)
    {
        _agentKey = Require(agentKey, nameof(agentKey));
        _displayName = Require(displayName, nameof(displayName));
    }

    public void SetModel(string model, AgentReasoningLevel reasoningLevel = AgentReasoningLevel.Low)
    {
        _model = Require(model, nameof(model));
        _reasoningLevel = reasoningLevel;
    }

    public void SetResult(AgentResultDefinition? result) => _result = result;
    public void SetLimits(AgentExecutionLimits limits) => _limits = limits ?? throw new ArgumentNullException(nameof(limits));

    public void AddInstruction(string key, string text, int order) => AddInstruction(key, text, order, null);

    public void AddInstruction(string key, string text, int order, string? compatibilityText)
    {
        key = Require(key, nameof(key));
        if (_instructions.ContainsKey(key))
            throw new InvalidOperationException($"Instruction '{key}' was added more than once.");
        _instructions.Add(key, new(key, Require(text, nameof(text)), order, compatibilityText));
    }

    public void ReplaceInstruction(string key, string text, int order) =>
        _instructions[Require(key, nameof(key))] = new(key, Require(text, nameof(text)), order);

    public void RemoveInstruction(string key) => _ = _instructions.Remove(Require(key, nameof(key)));

    public void AddCompatibilityMaterial(string key, string value)
    {
        key = Require(key, nameof(key));
        if (_compatibilityMaterials.ContainsKey(key))
            throw new InvalidOperationException($"Compatibility material '{key}' was added more than once.");
        _compatibilityMaterials.Add(key, Require(value, nameof(value)));
    }

    public void AddCapability(string capabilityKey)
    {
        capabilityKey = Require(capabilityKey, nameof(capabilityKey));
        if (_capabilities.Contains(capabilityKey, StringComparer.Ordinal))
            throw new InvalidOperationException($"Capability '{capabilityKey}' was added more than once.");
        _capabilities.Add(capabilityKey);
    }

    public void ReplaceCapabilities(IEnumerable<string> capabilityKeys)
    {
        _capabilities.Clear();
        foreach (string key in capabilityKeys) AddCapability(key);
    }

    public void RemoveCapability(string capabilityKey) => _capabilities.Remove(Require(capabilityKey, nameof(capabilityKey)));

    internal ResolvedAgentProfile<TContext> Build() => new(
        Require(_agentKey, "agentKey"), Require(_displayName, "displayName"), Require(_model, "model"),
        _reasoningLevel, _result, ValidateLimits(_limits),
        _instructions.Values.OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray(),
        _capabilities.ToArray(), _layers.ToArray(),
        _compatibilityMaterials.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));

    private static AgentExecutionLimits ValidateLimits(AgentExecutionLimits limits)
    {
        if (limits.MaxIterations < 1 || limits.MaxToolCalls < 0 || limits.MaxTokens < 1 || limits.EffectiveTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("Agent execution limits must be positive and bounded.");
        return limits;
    }

    private static string Require(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Profile value '{name}' is required.") : value;
}

public sealed record AgentProfileLayer(string Key, int Revision);
public sealed record InstructionSection(string Key, string Text, int Order, string? CompatibilityText = null);
public sealed record ResolvedAgentProfile<TContext>(
    string AgentKey, string DisplayName, string Model, AgentReasoningLevel ReasoningLevel,
    AgentResultDefinition? Result, AgentExecutionLimits Limits, IReadOnlyList<InstructionSection> Instructions,
    IReadOnlyList<string> CapabilityKeys, IReadOnlyList<AgentProfileLayer> Layers,
    IReadOnlyDictionary<string, string> CompatibilityMaterials);
