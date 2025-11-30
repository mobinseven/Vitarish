using Vorn.Communications.Wrappers.SmsDotIr;

namespace Vorn.Communications.Options;

public record VornOptions
{
    public const string Section = nameof(Vorn);

    public ProvidersOptions Providers { get; set; } = new();
    public AaasOptions Aaas { get; set; } = new();
}

public record AaasOptions
{
    public string Authority { get; set; } = string.Empty;
}

public record SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record ProvidersOptions
{
    public SmsDotIrOptions SmsDotIr { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
}
