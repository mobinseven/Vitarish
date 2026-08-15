namespace Vitarish.Main.Infrastructure.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public List<string> AllowedOrigins { get; set; } = [];
}

