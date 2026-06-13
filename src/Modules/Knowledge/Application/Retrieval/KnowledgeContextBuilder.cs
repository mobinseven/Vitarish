using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Retrieval;

public sealed class KnowledgeContextBuilder : IKnowledgeContextBuilder
{
    public KnowledgeContext Build(string question, IReadOnlyList<KnowledgeHit> hits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        StringBuilder builder = new();
        foreach (KnowledgeHit hit in hits)
        {
            _ = builder.AppendLine($"[chunk: {hit.ChunkId}]");
            _ = builder.AppendLine($"Title: {hit.Title}");
            _ = builder.AppendLine($"Source: {hit.SourceId}");
            _ = builder.AppendLine("Text:");
            _ = builder.AppendLine(hit.Text);
            _ = builder.AppendLine();
        }
        return new KnowledgeContext(question, builder.ToString(), [.. hits.Select(static hit => hit.ChunkId)]);
    }
}
