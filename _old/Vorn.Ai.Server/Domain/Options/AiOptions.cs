namespace Vorn.Ai.Server.Domain.Options;

public class AiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
    public string ResponseModel { get; set; } = "gpt-5-nano-2025-08-07";
    public string McpInstructions { get; set; } = string.Empty;
    public string QuickRewriteInstructions { get; set; } = @"
Rewrite the provided text in a quick, clear manner while retaining the original meaning. 
The rewrite should be in the same language as the input text. 
Analyze the text for core meaning and key details before writing the revised version. 

- Do not skip essential information.
- Use fluent, natural phrasing.

Output Format:
Return only the rewritten text, with no extra commentary or preamble.";
}
