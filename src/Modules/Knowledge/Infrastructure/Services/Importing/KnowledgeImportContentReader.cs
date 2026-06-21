using System.Globalization;
using System.Text.Json;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

internal sealed class KnowledgeImportContentReader
{
    private static readonly HashSet<string> TextFormats = new(StringComparer.OrdinalIgnoreCase) { "plain-text", "markdown" };
    private static readonly HashSet<string> JsonEvaluationFormats = new(StringComparer.OrdinalIgnoreCase) { "techqa-json", "rag-eval-json" };
    private static readonly HashSet<string> PlainQaJsonFormats = new(StringComparer.OrdinalIgnoreCase) { "plain-qa-json" };

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

        if (PlainQaJsonFormats.Contains(format))
        {
            return await ReadPlainQaJsonAsync(content, format, sourceName, cancellationToken);
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

    private static async Task<KnowledgeImportContent> ReadPlainQaJsonAsync(Stream content, string format, string sourceName, CancellationToken cancellationToken)
    {
        using JsonDocument json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        IReadOnlyList<JsonElement> rows = ReadRows(json.RootElement);
        if (rows.Count == 0)
        {
            throw new ArgumentException($"Knowledge import format '{format}' requires at least one JSON item.");
        }

        List<KnowledgeDocumentDraft> documents = [];
        List<KnowledgeQuestionDraft> questions = [];
        int rowNumber = 1;
        foreach (JsonElement row in rows)
        {
            string evaluationItemKey = $"{sourceName}:{rowNumber.ToString(CultureInfo.InvariantCulture)}";
            string? answer = FirstString(row, "answer", "expected_answer", "ground_truth", "reference_answer", "output");
            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new ArgumentException($"Knowledge import format '{format}' item {rowNumber} requires an answer, expected_answer, ground_truth, reference_answer, or output value.");
            }

            string? questionText = FirstString(row, "question", "query", "input", "prompt");
            if (string.IsNullOrWhiteSpace(questionText))
            {
                throw new ArgumentException($"Knowledge import format '{format}' item {rowNumber} requires a question, query, input, or prompt value.");
            }

            KnowledgeQuestionDraft question = new()
            {
                Question = questionText,
                EvaluationItemKey = evaluationItemKey,
                ExpectedAnswer = answer,
                ExpectedContextTexts = []
            };
            questions.Add(question);

            documents.Add(new KnowledgeDocumentDraft
            {
                EvaluationItemKey = evaluationItemKey,
                Title = ReadPlainQaTitle(row, sourceName, rowNumber),
                Text = answer,
                Product = FirstString(row, "product"),
                Version = FirstString(row, "version"),
                Language = FirstString(row, "language", "lang")
            });

            rowNumber++;
        }

        return new KnowledgeImportContent(documents, questions);
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
            string evaluationItemKey = $"{sourceName}:{rowNumber.ToString(CultureInfo.InvariantCulture)}";

            KnowledgeQuestionDraft question = ReadQuestion(row, format, rowNumber) with
            {
                EvaluationItemKey = evaluationItemKey
            };
            questions.Add(question);

            string? documentText = ReadRowLocalEvidenceText(row);
            if (!string.IsNullOrWhiteSpace(documentText))
            {
                documents.Add(new KnowledgeDocumentDraft
                {
                    EvaluationItemKey = evaluationItemKey,
                    Title = ReadTitle(row, sourceName, rowNumber),
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
            ExpectedContextTexts = ReadExpectedContexts(row)
        };
    }

    private static IReadOnlyList<string> ReadExpectedContexts(JsonElement row)
    {
        List<string> contexts = [];
        AddStringArray(contexts, row, "expected_contexts");
        AddStringArray(contexts, row, "contexts");
        AddStringArray(contexts, row, "context");
        AddStringArray(contexts, row, "documents");
        AddStringArray(contexts, row, "passages");
        string? single = FirstString(row, "expected_context", "document", "passage", "content");
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
                string? text = FirstString(item, "text", "content", "document", "context", "passage", "body", "abstract", "title");
                if (!string.IsNullOrWhiteSpace(text)) values.Add(text);
            }
        }
    }

    private static string ReadTitle(JsonElement row, string sourceName, int rowNumber) =>
        FirstString(row, "title", "document_title", "source") ?? $"{sourceName} item {rowNumber.ToString(CultureInfo.InvariantCulture)}";

    private static string ReadPlainQaTitle(JsonElement row, string sourceName, int rowNumber)
    {
        string itemIdentifier = FirstString(row, "id") ?? rowNumber.ToString(CultureInfo.InvariantCulture);
        return $"{sourceName} item {itemIdentifier}";
    }

    private static string? ReadRowLocalEvidenceText(JsonElement row) =>
        FirstEvidenceValue(row, "document", "context", "passage", "content", "contexts", "documents", "passages", "text");

    private static string? FirstEvidenceValue(JsonElement row, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!row.TryGetProperty(propertyName, out JsonElement property)) continue;

            string? text = ReadEvidenceValue(property);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        return null;
    }

    private static string? ReadEvidenceValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return FirstString(value, "text", "content", "document", "context", "passage", "body", "abstract", "title");
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return ReadString(value);
        }

        string[] values = [.. value.EnumerateArray()
            .Select(ReadEvidenceArrayItem)
            .Where(static text => !string.IsNullOrWhiteSpace(text))!];

        return values.Length == 0 ? null : string.Join("\n\n", values);
    }

    private static string? ReadEvidenceArrayItem(JsonElement item) => item.ValueKind switch
    {
        JsonValueKind.String => item.GetString(),
        JsonValueKind.Object => FirstString(item, "text", "content", "document", "context", "passage", "body", "abstract", "title"),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => item.ToString(),
        _ => null
    };

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
