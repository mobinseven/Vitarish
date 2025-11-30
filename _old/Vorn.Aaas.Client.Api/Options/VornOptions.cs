namespace Vorn.Aaas.Client.Api.Options;

public class VornOptions
{
    public const string Section = nameof(Vorn);
    public required AaasOptions Aaas { get; set; }
}
