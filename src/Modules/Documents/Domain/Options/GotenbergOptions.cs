namespace Vitarish.Documents.Domain.Options;

public class GotenbergOptions
{
    public string ServiceUrl { get; set; } = "http://localhost:3000";
    public int TimeoutSeconds { get; set; } = 180;
}
