namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalNotePayload
{
    public required string NoteType { get; init; }
    public string? NoteTypeDisplay { get; init; }
    public required string Content { get; init; }
    public string? ContentType { get; init; } = "text/plain";
    public string? AuthorFhirReference { get; init; }
    public string? Title { get; init; }
    public string? Status { get; init; } = "current";
}
