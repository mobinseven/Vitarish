using Vitarish.Clinic.Application.Assistant.Operations;

namespace Vitarish.Clinic.Application.Assistant.Capabilities;

public static class ContentsAssistantActionPolicy
{
    private const string AuthorDraft = "contents_author_draft";
    private const string Workflow = "contents";

    public static AssistantActionPolicy Create(string createTool, string saveTool) => new(
        AuthorDraft,
        Workflow,
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal) { AssistantOperationsScope.TenantOwnerRole },
        AssistantActionExecutionMode.DirectAllowlisted,
        1,
        null,
        AssistantActionRetryClassification.NeverRetry,
        new HashSet<string>(StringComparer.Ordinal) { createTool, saveTool });
}
