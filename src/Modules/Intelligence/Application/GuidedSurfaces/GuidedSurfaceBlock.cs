using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Application.GuidedSurfaces;

/// <summary>The platform block kinds of a guided surface (ADR-0022).</summary>
public enum GuidedSurfaceBlockKind
{
    Question,
    NextAction,
    ActionGroup
}

/// <summary>
/// One predesigned component in a reply. Exactly the member that matches <see cref="Kind"/> is set. Blocks built by
/// a tool carry server IDs only; the web reads each record's current state. Only a model-produced question
/// carries display text, and it has passed <see cref="AgentInteractiveReplyFormat"/> validation.
/// </summary>
public sealed record GuidedSurfaceBlock
{
    private GuidedSurfaceBlock(GuidedSurfaceBlockKind kind) => Kind = kind;

    public GuidedSurfaceBlockKind Kind { get; }
    public GuidedSurfaceQuestion? Question { get; private init; }
    public string? NextAction { get; private init; }
    public GuidedSurfaceActionGroup? ActionGroup { get; private init; }

    /// <summary>A question the model wrote; prompt and labels are required and already validated.</summary>
    public static GuidedSurfaceBlock ModelQuestion(AgentInteractiveQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        return new(GuidedSurfaceBlockKind.Question)
        {
            Question = new(question.Prompt, question.Options.Select(option => new GuidedSurfaceOption(option.Key, option.Label)).ToArray(), question.MaxSelections)
        };
    }

    public static GuidedSurfaceBlock NextActionKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new(GuidedSurfaceBlockKind.NextAction) { NextAction = key };
    }

    /// <summary>The actions one run prepared, by ID; the web reads each action's current state.</summary>
    public static GuidedSurfaceBlock PreparedActions(string agentRunId, IReadOnlyList<Guid> actionIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentRunId);
        ArgumentNullException.ThrowIfNull(actionIds);
        if (actionIds.Count == 0)
            throw new ArgumentException("An action group needs at least one action.", nameof(actionIds));
        return new(GuidedSurfaceBlockKind.ActionGroup) { ActionGroup = new(agentRunId, actionIds.Distinct().ToArray()) };
    }

}

public sealed record GuidedSurfaceQuestion(string Prompt, IReadOnlyList<GuidedSurfaceOption> Options, int MaxSelections);

public sealed record GuidedSurfaceOption(string Key, string Label);

public sealed record GuidedSurfaceActionGroup(string AgentRunId, IReadOnlyList<Guid> ActionIds);
