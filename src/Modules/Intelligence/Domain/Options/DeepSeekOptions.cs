namespace Vitarish.Intelligence.Domain.Options;

public class DeepSeekOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
}
