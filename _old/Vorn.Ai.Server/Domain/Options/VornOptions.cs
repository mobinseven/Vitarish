namespace Vorn.Ai.Server.Domain.Options;

public class VornOptions
{
    public const string Section = "Vorn";
    public AiOptions Ai { get; set; }
    public AaasOptions Aaas { get; set; }
}
