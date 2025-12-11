namespace Vitarish.Intelligence.Domain.Options;

public class GoogleSpeechOptions
{
    public string ProjectId { get; set; } = string.Empty;

    public string Location { get; set; } = "us-central1";

    public string RecognizerId { get; set; } = "default";

    public string? CredentialsPath { get; set; }
        = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

    public string DefaultLanguageCode { get; set; } = "en-US";

    public string DefaultModelId { get; set; } = "long";

    public IReadOnlyCollection<string>? SupportedModels { get; set; }
        = new[] { "long", "short", "chirp", "chirp-v2", "medical_dictation" };
}
