using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Answering;

public sealed class IntelligenceKnowledgeAnswerer(IKnowledgeRetriever retriever, IKnowledgeContextBuilder contextBuilder, IResponder responder, IKnowledgeRepository repository) : IKnowledgeAnswerer
{
    public async Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);

        IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(new KnowledgeQuery { CorpusId = request.CorpusId, Question = request.Question, TenantId = request.TenantId, TopK = request.TopK }, cancellationToken);
        KnowledgeContext context = contextBuilder.Build(request.Question, hits);
        string prompt = $"Question:\n{request.Question}\n\nContext:\n{context.Text}";
        IReadOnlyList<ResponderOutput> outputs = await responder.GenerateAsync([ResponderInput.TextInput(prompt)], new ResponderOptions { Instructions = KnowledgeGroundingPolicy.Instructions, TextFormat = ResponseTextFormat.JsonObject }, cancellationToken);
        string json = ExtractText(outputs);
        KnowledgeAnswer answer = MapAnswer(json, hits);

        await repository.SaveAskTraceAsync(new KnowledgeAskTrace
        {
            Id = Guid.NewGuid().ToString("n"),
            CorpusId = request.CorpusId,
            Question = request.Question,
            RetrievedChunkIds = context.ChunkIds,
            GeneratedAnswer = answer.Answer,
            IsAnswered = answer.IsAnswered,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return answer;
    }

    private static string ExtractText(IReadOnlyList<ResponderOutput> outputs) => string.Concat(Flatten(outputs).Select(static output => output.Text));

    private static IEnumerable<ResponderOutput> Flatten(IEnumerable<ResponderOutput> outputs)
    {
        foreach (ResponderOutput output in outputs)
        {
            yield return output;
            if (output.Content is not null)
            {
                foreach (ResponderOutput child in Flatten(output.Content)) yield return child;
            }
        }
    }

    private static KnowledgeAnswer MapAnswer(string json, IReadOnlyList<KnowledgeHit> hits)
    {
        GeneratedKnowledgeAnswer? generated = JsonSerializer.Deserialize<GeneratedKnowledgeAnswer>(json, JsonOptions) ?? throw new InvalidOperationException("Knowledge answer generation returned empty JSON.");
        Dictionary<string, KnowledgeHit> byChunk = new(StringComparer.Ordinal);
        foreach (KnowledgeHit hit in hits)
        {
            byChunk[hit.ChunkId] = hit;
        }

        return new KnowledgeAnswer
        {
            Answer = generated.Answer ?? string.Empty,
            IsAnswered = generated.IsAnswered,
            MissingInformation = generated.MissingInformation,
            RetrievedContext = hits,
            Citations = generated.Citations is null ? [] : [.. generated.Citations.Where(citation => citation.ChunkId is not null && byChunk.ContainsKey(citation.ChunkId)).Select(citation =>
            {
                KnowledgeHit hit = byChunk[citation.ChunkId!];
                return new KnowledgeCitation { ChunkId = hit.ChunkId, SourceId = hit.SourceId, DocumentId = hit.DocumentId, Title = hit.Title, Quote = citation.Quote };
            })]
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed record GeneratedKnowledgeAnswer(string? Answer, bool IsAnswered, IReadOnlyList<GeneratedCitation>? Citations, string? MissingInformation);
    private sealed record GeneratedCitation([property: JsonPropertyName("chunkId")] string? ChunkId, string? Quote);
}
