namespace Vitarish.Knowledge.Contracts.Routes;

public static class KnowledgeApiRoutes
{
    public const string Base = "/api/modules/knowledge";
    public const string OwnerWorkspace = "/owner/workspace";
    public const string Corpora = "/corpora";
    public const string Sources = "/sources";
    public const string SourceFiles = "/sources/files";
    public const string ReplaceSource = "/sources/replace";
    public const string ReplaceSourceFile = "/sources/files/replace";
    public const string DeleteSource = "/sources/delete";
    public const string Indexing = "/indexing";
    public const string Ask = "/ask";
    public const string Evaluation = "/evaluation";
    public const string DiagnosticsCorpora = "/diagnostics/corpora";
    public const string DiagnosticsCorpus = "/diagnostics/corpus";
    public const string DiagnosticsSources = "/diagnostics/sources";
    public const string DiagnosticsChunks = "/diagnostics/chunks";
    public const string DiagnosticsAskTraces = "/diagnostics/ask-traces";
    public const string DiagnosticsInspect = "/diagnostics/inspect";
    public const string DiagnosticsDebugAsk = "/diagnostics/debug-ask";
    public const string DiagnosticsIndex = "/diagnostics/index";
}
