using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Infrastructure.Services.Answering;

public sealed class IntelligenceKnowledgeAnswerer(IKnowledgeRetriever retriever, IKnowledgeContextBuilder contextBuilder, IResponder responder, IKnowledgeAskTraceRepository repository, ILogger<IntelligenceKnowledgeAnswerer> logger) : IKnowledgeAnswerer
{
    public async Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);

        IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(new KnowledgeQuery
        {
            CorpusId = request.CorpusId,
            Question = request.Question,
            TenantId = request.TenantId,
            TopK = request.TopK
        }, cancellationToken);
        return await AnswerFromHitsAsync(request, hits, cancellationToken);
    }

    public async Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentNullException.ThrowIfNull(hits);
        if (hits.Count == 0)
        {
            KnowledgeAnswer refused = RefusedAnswer(hits, "No relevant context was retrieved.");
            await SaveTraceAsync(request, [], refused, cancellationToken);
            return refused;
        }

        KnowledgeContext context = contextBuilder.Build(request.Question, hits);
        string prompt = $"Question:\n{request.Question}\n\nContext:\n{context.Text}";
        IReadOnlyList<ResponderOutput> outputs = await responder.GenerateAsync([ResponderInput.TextInput(prompt)], new ResponderOptions
        {
            Instructions = KnowledgeGroundingPolicy.Instructions,
            TextFormat = ResponseTextFormat.JsonSchema,
            JsonSchema = KnowledgeAnswerSchema
        }, cancellationToken);
        string json = ExtractText(outputs);
        KnowledgeAnswer answer = MapAnswer(json, hits);

        await SaveTraceAsync(request, context.ChunkIds, answer, cancellationToken);

        return answer;
    }


    private async Task SaveTraceAsync(KnowledgeAskRequest request, IReadOnlyList<Guid> retrievedChunkIds, KnowledgeAnswer answer, CancellationToken cancellationToken)
    {
        if (!request.SaveTrace)
        {
            return;
        }

        try
        {
            await repository.SaveAskTraceAsync(new KnowledgeAskTrace
            {
                Id = Guid.NewGuid(),
                CorpusId = request.CorpusId,
                TenantId = request.TenantId,
                Question = request.Question,
                RetrievedChunkIds = retrievedChunkIds,
                GeneratedAnswer = answer.Answer,
                MissingInformation = answer.MissingInformation,
                CitationsJson = JsonSerializer.Serialize(answer.Citations, JsonOptions),
                TraceKind = request.TraceKind,
                IsAnswered = answer.IsAnswered,
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Knowledge ask trace persistence failed for corpus {CorpusId} and tenant {TenantId}.", request.CorpusId, request.TenantId);
        }
    }
    private static string ExtractText(IReadOnlyList<ResponderOutput> outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        // First prefer any single output node that already contains the complete JSON.
        // This avoids concatenating duplicate parent/child text.
        foreach (string candidate in EnumerateText(outputs)
            .Select(static text => text.Trim())
            .Where(static text => text.Length > 0)
            .Distinct(StringComparer.Ordinal))
        {
            if (IsKnowledgeAnswerJson(candidate))
            {
                return candidate;
            }
        }

        // If the responder returned fragmented text, concatenate only leaf text nodes.
        // This avoids parent + child duplication while still supporting split output.
        string leafText = string.Concat(EnumerateLeafText(outputs)).Trim();
        if (IsKnowledgeAnswerJson(leafText))
        {
            return leafText;
        }

        // Last safe fallback: top-level text only, never recursive flattening.
        string topLevelText = string.Concat(outputs.Select(static output => output.Text ?? string.Empty)).Trim();
        if (IsKnowledgeAnswerJson(topLevelText))
        {
            return topLevelText;
        }

        throw new InvalidOperationException("Knowledge answer generation did not contain a valid single knowledge-answer JSON object.");
    }

    private static IEnumerable<string> EnumerateText(IEnumerable<ResponderOutput> outputs)
    {
        foreach (ResponderOutput output in outputs)
        {
            if (!string.IsNullOrWhiteSpace(output.Text))
            {
                yield return output.Text;
            }

            if (output.Content is null)
            {
                continue;
            }

            foreach (string childText in EnumerateText(output.Content))
            {
                yield return childText;
            }
        }
    }

    private static IEnumerable<string> EnumerateLeafText(IEnumerable<ResponderOutput> outputs)
    {
        foreach (ResponderOutput output in outputs)
        {
            bool yieldedChildText = false;

            if (output.Content is not null)
            {
                foreach (string childText in EnumerateLeafText(output.Content))
                {
                    yieldedChildText = true;
                    yield return childText;
                }
            }

            if (!yieldedChildText && !string.IsNullOrWhiteSpace(output.Text))
            {
                yield return output.Text;
            }
        }
    }

    private static bool IsKnowledgeAnswerJson(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonElement root = document.RootElement;

            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("answer", out _)
                && root.TryGetProperty("isAnswered", out _)
                && root.TryGetProperty("missingInformation", out _)
                && root.TryGetProperty("citations", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static KnowledgeAnswer MapAnswer(string json, IReadOnlyList<KnowledgeHit> hits)
    {
        GeneratedKnowledgeAnswer? generated;
        try
        {
            generated = JsonSerializer.Deserialize<GeneratedKnowledgeAnswer>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Knowledge answer generation returned invalid JSON.", ex);
        }

        if (generated is null)
        {
            throw new InvalidOperationException("Knowledge answer generation returned empty JSON.");
        }
        Dictionary<Guid, KnowledgeHit> byChunk = [];
        foreach (KnowledgeHit hit in hits)
        {
            byChunk[hit.ChunkId] = hit;
        }

        List<KnowledgeCitation> citations = [];
        foreach (GeneratedCitation citation in generated.Citations ?? [])
        {
            if (!Guid.TryParse(citation.ChunkId, out Guid citationChunkId) || !byChunk.TryGetValue(citationChunkId, out KnowledgeHit? hit))
            {
                return RefusedAnswer(hits, "The generated answer cited context chunks that were not retrieved.");
            }

            citations.Add(new KnowledgeCitation
            {
                ChunkId = hit.ChunkId,
                SourceId = hit.SourceId,
                DocumentId = hit.DocumentId,
                Title = hit.Title,
                Quote = citation.Quote
            });
        }

        return generated.IsAnswered && citations.Count == 0
            ? RefusedAnswer(hits, "The generated answer did not include citations.")
            : new KnowledgeAnswer
            {
                Answer = generated.Answer ?? string.Empty,
                IsAnswered = generated.IsAnswered,
                MissingInformation = generated.MissingInformation,
                RetrievedContext = hits,
                Citations = citations
            };
    }

    private static KnowledgeAnswer RefusedAnswer(IReadOnlyList<KnowledgeHit> hits, string reason) => new()
    {
        Answer = string.Empty,
        IsAnswered = false,
        MissingInformation = reason,
        RetrievedContext = hits,
        Citations = []
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly object NullableStringSchema = new { anyOf = new object[] { new { type = "string" }, new { type = "null" } } };

    private static readonly ResponderJsonSchema KnowledgeAnswerSchema = new()
    {
        Name = "knowledge_answer",
        Description = "Grounded knowledge answer with citations to retrieved chunks.",
        Strict = true,
        Schema = JsonSerializer.Serialize(new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                answer = new { type = "string" },
                isAnswered = new { type = "boolean" },
                missingInformation = NullableStringSchema,
                citations = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            chunkId = new { type = "string" },
                            quote = NullableStringSchema
                        },
                        required = new[] { "chunkId", "quote" }
                    }
                }
            },
            required = new[] { "answer", "isAnswered", "missingInformation", "citations" }
        }, JsonOptions)
    };

    private sealed record GeneratedKnowledgeAnswer(string? Answer, bool IsAnswered, IReadOnlyList<GeneratedCitation>? Citations, string? MissingInformation);
    private sealed record GeneratedCitation([property: JsonPropertyName("chunkId")] string? ChunkId, string? Quote);
}
