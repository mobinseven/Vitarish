namespace Vitarish.Intelligence.Domain.Options;

public class AiOptions
{
    public string DefaultTranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
    public string DefaultResponseModel { get; set; } = "gpt-5-nano-2025-08-07";
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public GoogleOptions Google { get; set; } = new();
    public DeepSeekOptions DeepSeek { get; set; } = new();
    public string QuickRewriteInstructions { get; set; } = @"
Rewrite the provided text in a quick, clear manner while retaining the original meaning. 
The rewrite should be in the same language as the input text. 
Analyze the text for core meaning and key details before writing the revised version. 

- Do not skip essential information.
- Use fluent, natural phrasing.

Output Format:
Return only the rewritten text, with no extra commentary or preamble.";
}
