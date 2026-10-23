namespace Vitarish.Intelligence.Contracts;

/// <summary>An agent reply that any app renders as text, one question with options, references and one next step.</summary>
public sealed record AgentInteractiveReplyResponse(
    string Text,
    AgentInteractiveQuestionResponse? Question,
    IReadOnlyList<string> References,
    string? NextAction,
    IReadOnlyList<AgentRememberedResponse>? Remembered = null);

/// <summary>A fact the app saved from this reply; the person can undo it by its id.</summary>
public sealed record AgentRememberedResponse(string Id, string Text);

public sealed record AgentInteractiveQuestionResponse(
    string Prompt,
    IReadOnlyList<AgentInteractiveOptionResponse> Options,
    int MaxSelections);

public sealed record AgentInteractiveOptionResponse(string Key, string Label);
