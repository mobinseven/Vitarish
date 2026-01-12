namespace Vitarish.Interfaces.Base.Models;

public record Breadcrumb
{
    public string Text { get; set; }
    public string Path { get; set; }
    public Breadcrumb(string Path, string Text)
    {
        this.Path = Path;
        this.Text = Text;
    }
}
