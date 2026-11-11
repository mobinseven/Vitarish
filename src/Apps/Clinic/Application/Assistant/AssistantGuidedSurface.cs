using Vitarish.Clinic.Application.Setup;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Clinic.Application.Assistant;

/// <summary>The next steps the operations assistant may suggest: the screen of a setup goal.</summary>
public static class AssistantNextActions
{
    public static IReadOnlyCollection<string> Keys { get; } = Enum.GetNames<ClinicSetupGoal>();
}

/// <summary>Turns one assistant reply into guided-surface blocks (ADR-0022).</summary>
public static class AssistantGuidedSurface
{
    public static IReadOnlyList<GuidedSurfaceBlock> Blocks(AssistantResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        List<GuidedSurfaceBlock> blocks = [];
        if (result.Actions.Count > 0)
            blocks.Add(GuidedSurfaceBlock.PreparedActions(result.AgentRunId, result.Actions.Select(action => action.ActionId).ToArray()));
        if (result.Question is { } question)
            blocks.Add(GuidedSurfaceBlock.ModelQuestion(question));
        if (result.NextAction is { } nextAction)
            blocks.Add(GuidedSurfaceBlock.NextActionKey(nextAction));
        return blocks;
    }
}
