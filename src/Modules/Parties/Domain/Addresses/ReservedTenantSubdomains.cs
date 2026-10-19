namespace Vitarish.Parties.Domain.Addresses;

/// <summary>
/// Subdomains no tenant may claim: the platform's own hosts, sign-in and onboarding
/// words, and names that mail, DNS, and certificate tooling treat specially.
/// </summary>
public static class ReservedTenantSubdomains
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        // Platform hosts and surfaces
        "vitarish", "clinic", "clinics", "app", "apps", "admin", "administrator", "platform",
        "landing", "patients", "patient", "pm", "start", "onboarding", "setup", "signup",
        "register", "join", "login", "logout", "signin", "signout", "auth", "account",
        "accounts", "identity", "sso", "oauth", "api", "graphql", "internal", "agent",
        "agents", "assistant",
        // Environments
        "e2e", "test", "testing", "staging", "stage", "dev", "development", "demo", "sandbox",
        "preview", "beta", "prod", "production", "local", "localhost",
        // Web, operations, and support
        "www", "web", "cdn", "static", "assets", "files", "media", "images", "img", "uploads",
        "download", "downloads", "status", "health", "metrics", "monitor", "monitoring",
        "logs", "grafana", "hangfire", "support", "help", "docs", "blog", "news", "about",
        "contact", "billing", "pay", "payment", "payments", "security", "privacy", "terms",
        "legal", "abuse", "root", "system", "sysadmin", "webmaster", "hostmaster",
        "postmaster", "info", "noreply", "no-reply",
        // Mail, DNS, and certificate tooling
        "mail", "email", "smtp", "imap", "pop", "pop3", "mx", "webmail", "autodiscover",
        "autoconfig", "ns", "ns1", "ns2", "ns3", "ns4", "dns", "ftp", "sftp", "ssh", "vpn",
        "proxy", "gateway", "acme", "ssl", "tls", "wpad", "isatap"
    };

    public static bool Contains(string subdomain) => Names.Contains(subdomain);
}
