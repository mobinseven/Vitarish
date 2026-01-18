namespace Vitarish.Interfaces.Base.Models;

public record Breadcrumb
{
    public string Text { get; set; }
    public string Path { get; set; }
    public string Id { get; set; }
}
