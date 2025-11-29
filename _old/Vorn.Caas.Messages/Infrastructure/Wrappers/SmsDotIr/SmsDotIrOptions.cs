namespace Vorn.Caas.Messages.Infrastructure.Wrappers.SmsDotIr;

public record SmsDotIrOptions
{
    public string Key { get; set; } = string.Empty;
    public string LineNumber { get; set; } = string.Empty;
    public string VerificationCodeTemplateId { get; set; } = string.Empty;
}

