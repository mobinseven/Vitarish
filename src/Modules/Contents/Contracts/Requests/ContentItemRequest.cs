using Vitarish.Contents.Contracts.Models;

namespace Vitarish.Contents.Contracts.Requests;

public sealed record ContentItemRequest(ContentItemType Type, ContentStatus Status, string Slug,
    string CanonicalPath, string Title, string Excerpt, IReadOnlyList<ContentBodyBlock> Body, string Locale);
