namespace Vitarish.Modules.Base.Interface.Models;

public record Breadcrumb
{
    public required string Text { get; set; }
    public required string Path { get; set; }
    public required string Id { get; set; }
}
