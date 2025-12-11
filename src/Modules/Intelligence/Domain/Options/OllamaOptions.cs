namespace Vitarish.Intelligence.Domain.Options;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "llama3.2";
    public string DefaultTranscriberModel { get; set; } = "whisper";
}
