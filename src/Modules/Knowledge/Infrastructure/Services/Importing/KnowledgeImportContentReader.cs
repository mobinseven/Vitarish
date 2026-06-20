using System.Text.Json;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

internal sealed class KnowledgeImportContentReader
{
    private static readonly HashSet<string> TextFormats = new(StringComparer.OrdinalIgnoreCase) { "plain-text", "markdown" };
    private static readonly HashSet<string> JsonEvaluationFormats = new(StringComparer.OrdinalIgnoreCase) { "techqa-json", "rag-eval-json" };

    public async Task<KnowledgeImportContent> ReadAsync(Stream content, string format, string sourceName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        if (TextFormats.Contains(format))
        {
            return await ReadTextAsync(content, sourceName, cancellationToken);
        }

        if (JsonEvaluationFormats.Contains(format))
        {
            return await ReadEvaluationJsonAsync(content, format, sourceName, cancellationToken);
        }

        throw new NotSupportedException($"Knowledge import format '{format}' is not supported by the MVP importer.");
    }

    private static async Task<KnowledgeImportContent> ReadTextAsync(Stream content, string sourceName, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(content, leaveOpen: true);
        string text = await reader.ReadToEndAsync(cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new KnowledgeImportContent([new KnowledgeDocumentDraft { Title = sourceName, Text = text }], []);
    }

    private static async Task<KnowledgeImportContent> ReadEvaluationJsonAsync(Stream content, string format, string sourceName, CancellationToken cancellationToken)
    {
        using JsonDocument json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        JsonElement root = json.RootElement;
        IReadOnlyList<JsonElement> rows = ReadRows(root);
        if (rows.Count == 0)
        {
            throw new ArgumentException($"Knowledge import format '{format}' requires at least one JSON item.");
        }

        List<KnowledgeDocumentDraft> documents = [];
        List<KnowledgeQuestionDraft> questions = [];
        int rowNumber = 1;
        foreach (JsonElement row in rows)
        {
            KnowledgeQuestionDraft question = ReadQuestion(row, format, rowNumber);
            questions.Add(question);

            string? documentText = FirstString(row, "document", "text", "context", "passage", "content") ?? FirstStringFromArray(row, "contexts", "context", "documents", "passages");
            if (!string.IsNullOrWhiteSpace(documentText))
            {
                documents.Add(new KnowledgeDocumentDraft
                {
                    Title = FirstString(row, "title", "document_title", "source") ?? $"{sourceName} item {rowNumber}",
                    Text = documentText,
                    Product = FirstString(row, "product"),
                    Version = FirstString(row, "version"),
                    Language = FirstString(row, "language", "lang")
                });
            }

            rowNumber++;
        }

        if (documents.Count == 0)
        {
            throw new ArgumentException($"Knowledge import format '{format}' requires document, text, context, contexts, or passages content.");
        }

        return new KnowledgeImportContent(documents, questions);
    }

    private static IReadOnlyList<JsonElement> ReadRows(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return [.. root.EnumerateArray()];
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (string propertyName in new[] { "data", "items", "examples", "questions", "records" })
            {
                if (root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.Array)
                {
                    return [.. value.EnumerateArray()];
                }
            }

            return [root];
        }

        throw new ArgumentException("Knowledge JSON import content must be a JSON object or array.");
    }

    private static KnowledgeQuestionDraft ReadQuestion(JsonElement row, string format, int rowNumber)
    {
        string? question = FirstString(row, "question", "query", "input", "prompt");
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException($"Knowledge import format '{format}' item {rowNumber} requires a question, query, input, or prompt value.");
        }

        return new KnowledgeQuestionDraft
        {
            Question = question,
            ExpectedAnswer = FirstString(row, "answer", "expected_answer", "ground_truth", "reference_answer", "output"),
            ExpectedContextTexts = ReadExpectedContexts(row),
            ExpectedChunkIds = ReadExpectedChunkIds(row)
        };
    }


    private static IReadOnlyList<Guid> ReadExpectedChunkIds(JsonElement row)
    {
        List<Guid> ids = [];
        AddGuidValues(ids, row, "expected_chunk_ids");
        AddGuidValues(ids, row, "expected_chunk_id");
        AddGuidValues(ids, row, "expected_chunks");
        AddGuidValues(ids, row, "gold_chunk_ids");
        AddGuidValues(ids, row, "gold_chunk_id");
        AddGuidValues(ids, row, "chunk_ids");
        AddGuidValues(ids, row, "chunk_id");
        AddGuidValues(ids, row, "evidence_chunk_ids");
        AddGuidValues(ids, row, "evidence_ids");
        return [.. ids.Distinct()];
    }

    private static void AddGuidValues(List<Guid> values, JsonElement row, string propertyName)
    {
        if (!row.TryGetProperty(propertyName, out JsonElement property)) return;
        AddGuidValue(values, property);
    }

    private static void AddGuidValue(List<Guid> values, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray()) AddGuidValue(values, item);
            return;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            string? objectId = FirstString(value, "id", "chunk_id", "chunkId", "expected_chunk_id", "evidence_id");
            if (Guid.TryParse(objectId, out Guid parsedObjectId)) values.Add(parsedObjectId);
            return;
        }

        string? text = ReadString(value);
        if (Guid.TryParse(text, out Guid parsedId)) values.Add(parsedId);
    }

    private static IReadOnlyList<string> ReadExpectedContexts(JsonElement row)
    {
        List<string> contexts = [];
        AddStringArray(contexts, row, "expected_contexts");
        AddStringArray(contexts, row, "contexts");
        AddStringArray(contexts, row, "context");
        AddStringArray(contexts, row, "documents");
        AddStringArray(contexts, row, "passages");
        string? single = FirstString(row, "expected_context", "document", "text", "passage", "content");
        if (!string.IsNullOrWhiteSpace(single)) contexts.Add(single);
        return [.. contexts.Where(static context => !string.IsNullOrWhiteSpace(context)).Distinct(StringComparer.Ordinal)];
    }

    private static void AddStringArray(List<string> values, JsonElement row, string propertyName)
    {
        if (!row.TryGetProperty(propertyName, out JsonElement property)) return;
        if (property.ValueKind == JsonValueKind.String)
        {
            values.Add(property.GetString() ?? string.Empty);
            return;
        }

        if (property.ValueKind != JsonValueKind.Array) return;
        foreach (JsonElement item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                values.Add(item.GetString() ?? string.Empty);
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                string? text = FirstString(item, "text", "content", "context", "passage", "document");
                if (!string.IsNullOrWhiteSpace(text)) values.Add(text);
            }
        }
    }

    private static string? FirstStringFromArray(JsonElement row, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!row.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Array) continue;
            string[] values = [.. property.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ValueKind == JsonValueKind.Object ? FirstString(item, "text", "content", "context", "passage", "document") : null).Where(static value => !string.IsNullOrWhiteSpace(value))!];
            if (values.Length > 0) return string.Join("\n\n", values);
        }

        return null;
    }

    private static string? FirstString(JsonElement row, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (row.TryGetProperty(propertyName, out JsonElement value))
            {
                string? text = ReadString(value);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
        _ => null
    };
}
