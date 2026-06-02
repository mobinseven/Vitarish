namespace Vitarish.Interfaces.Base.Models;

public record Breadcrumb
{
    public required string Text { get; set; }
    public required string Path { get; set; }
    public required string Id { get; set; }
}
