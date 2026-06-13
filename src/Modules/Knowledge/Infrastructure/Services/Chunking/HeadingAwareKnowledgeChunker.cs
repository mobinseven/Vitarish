using System.IO;
using System.Text;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Chunking;

public sealed class HeadingAwareKnowledgeChunker : IKnowledgeChunker
{
    private const int TargetCharacters = 2_400;
    private const int OverlapCharacters = 240;

    public IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(document.Text);

        List<KnowledgeChunkDraft> chunks = [];
        StringBuilder buffer = new();
        string? headingPath = null;
        int order = 0;

        using StringReader reader = new(document.Text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            bool isHeading = IsMarkdownHeading(line);
            if (isHeading && buffer.Length > 0)
            {
                AddChunk(chunks, document.Title, headingPath, buffer.ToString(), order++);
                buffer.Clear();
            }

            if (isHeading)
            {
                headingPath = line.Trim();
            }

            _ = buffer.AppendLine(line);

            if (buffer.Length >= TargetCharacters)
            {
                AddChunk(chunks, document.Title, headingPath, buffer.ToString(), order++);
                KeepOverlap(buffer);
            }
        }

        if (buffer.Length > 0)
        {
            AddChunk(chunks, document.Title, headingPath, buffer.ToString(), order);
        }

        return chunks;
    }

    private static bool IsMarkdownHeading(string line) => line.StartsWith('#') && line.TrimStart('#').StartsWith(' ');

    private static void AddChunk(List<KnowledgeChunkDraft> chunks, string title, string? headingPath, string text, int order)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0) return;
        chunks.Add(new KnowledgeChunkDraft { Text = trimmed, Order = order, Title = title, HeadingPath = headingPath });
    }

    private static void KeepOverlap(StringBuilder buffer)
    {
        if (buffer.Length <= OverlapCharacters) return;

        string text = buffer.ToString();
        buffer.Clear();
        _ = buffer.Append(text[^OverlapCharacters..]);
    }
}
