namespace Vorn.Ai.Server.Domain.Options
{
    public class GoogleOptions
    {
        public GoogleSpeechOptions Speech { get; set; } = new();
        public GoogleGeminiOptions Gemini { get; set; } = new();
    }
}
