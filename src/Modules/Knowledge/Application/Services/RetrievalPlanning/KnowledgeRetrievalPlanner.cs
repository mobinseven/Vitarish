using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Services.RetrievalPlanning;

public sealed class KnowledgeRetrievalPlanner(IResponder responder) : IKnowledgeRetrievalPlanner
{
    private const int PlannedQueryLimit = 5;
    private const int ExpansionWordThreshold = 24;
    private const int ExpansionCharacterThreshold = 160;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<KnowledgeRetrievalPlan> PlanAsync(string originalQuestion, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalQuestion);

        string cleanedQuery = KnowledgeQueryTextCleaner.Clean(originalQuestion);
        AiRetrievalPlan? aiPlan = ShouldExpand(cleanedQuery) ? await TryExpandAsync(originalQuestion, cancellationToken) : null;
        IReadOnlyList<string> searchQueries = MergeQueries(originalQuestion, cleanedQuery, aiPlan?.SearchQueries ?? []);

        return new KnowledgeRetrievalPlan
        {
            OriginalQuestion = originalQuestion,
            CleanedQuery = cleanedQuery,
            SearchQueries = searchQueries,
            PreservedTerms = NormalizeTerms(aiPlan?.PreservedTerms ?? []),
            ExpandedTerms = NormalizeTerms(aiPlan?.ExpandedTerms ?? []),
            DroppedTerms = NormalizeTerms(aiPlan?.DroppedTerms ?? []),
            Warnings = NormalizeTerms(aiPlan?.Warnings ?? [])
        };
    }

    private static bool ShouldExpand(string cleanedQuery) => cleanedQuery.Length >= ExpansionCharacterThreshold || cleanedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= ExpansionWordThreshold;

    private async Task<AiRetrievalPlan?> TryExpandAsync(string originalQuestion, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ResponderOutput> outputs = await responder.GenerateAsync([ResponderInput.TextInput(originalQuestion)], new ResponderOptions
            {
                Instructions = "Convert the user question into focused retrieval queries. Preserve user-supplied entities, features, symptoms, examples, and constraints. Add only plausible terminology variants. Do not add products, versions, platforms, causes, fixes, or resolutions not present in the question. Do not answer the question. Return only structured JSON.",
                TextFormat = ResponseTextFormat.JsonSchema,
                JsonSchema = RetrievalPlanSchema
            }, cancellationToken);

            string json = ExtractText(outputs);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AiRetrievalPlan { Warnings = ["AI retrieval expansion returned no content and was ignored."] };
            }

            return JsonSerializer.Deserialize<AiRetrievalPlan>(json, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException)
        {
            return new AiRetrievalPlan { Warnings = ["AI retrieval expansion returned invalid JSON and was ignored."] };
        }
        catch (Exception ex)
        {
            return new AiRetrievalPlan { Warnings = [$"AI retrieval expansion failed and was ignored: {ex.GetType().Name}."] };
        }
    }

    private static string ExtractText(IReadOnlyList<ResponderOutput> outputs) => string.Concat(outputs.SelectMany(Flatten).Where(output => !string.IsNullOrWhiteSpace(output.Text)).Select(output => output.Text));

    private static IEnumerable<ResponderOutput> Flatten(ResponderOutput output)
    {
        yield return output;
        if (output.Content is null)
        {
            yield break;
        }

        foreach (ResponderOutput child in output.Content.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static IReadOnlyList<string> MergeQueries(string originalQuestion, string cleanedQuery, IReadOnlyList<string> aiQueries)
    {
        List<string> planned = [];
        AddQuery(planned, originalQuestion);
        AddQuery(planned, cleanedQuery);
        foreach (string aiQuery in aiQueries)
        {
            AddQuery(planned, aiQuery);
            if (planned.Count >= PlannedQueryLimit)
            {
                break;
            }
        }

        return planned;
    }

    private static void AddQuery(List<string> planned, string query)
    {
        string normalized = KnowledgeQueryTextCleaner.Clean(query);
        if (planned.Any(existing => IsNearDuplicate(existing, normalized)))
        {
            return;
        }

        planned.Add(normalized);
    }

    private static bool IsNearDuplicate(string left, string right) => string.Equals(NearDuplicateKey(left), NearDuplicateKey(right), StringComparison.OrdinalIgnoreCase);

    private static string NearDuplicateKey(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static IReadOnlyList<string> NormalizeTerms(IReadOnlyList<string> terms) => [.. terms.Where(term => !string.IsNullOrWhiteSpace(term)).Select(term => KnowledgeQueryTextCleaner.Clean(term)).Distinct(StringComparer.OrdinalIgnoreCase)];

    private sealed record AiRetrievalPlan
    {
        public IReadOnlyList<string> SearchQueries { get; init; } = [];
        public IReadOnlyList<string> PreservedTerms { get; init; } = [];
        public IReadOnlyList<string> ExpandedTerms { get; init; } = [];
        public IReadOnlyList<string> DroppedTerms { get; init; } = [];
        public IReadOnlyList<string> Warnings { get; init; } = [];
    }

    private static readonly ResponderJsonSchema RetrievalPlanSchema = new()
    {
        Name = "knowledge_retrieval_plan",
        Description = "Focused retrieval queries and term accounting for a knowledge question.",
        Strict = true,
        Schema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "searchQueries": { "type": "array", "items": { "type": "string" } },
            "preservedTerms": { "type": "array", "items": { "type": "string" } },
            "expandedTerms": { "type": "array", "items": { "type": "string" } },
            "droppedTerms": { "type": "array", "items": { "type": "string" } },
            "warnings": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["searchQueries", "preservedTerms", "expandedTerms", "droppedTerms", "warnings"]
        }
        """
    };
}
