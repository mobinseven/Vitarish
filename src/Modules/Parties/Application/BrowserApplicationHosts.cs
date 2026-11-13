namespace Vitarish.Parties.Application;

/// <summary>Public hosts of platform Web applications that need their own tenant binding.</summary>
public sealed class BrowserApplicationHostOptions
{
    public string? PatientsUrl { get; set; }
    public string? ProjectManagementUrl { get; set; }
    public string? KnowledgeQaUrl { get; set; }

    public static bool IsDevelopmentHostname(string hostname) =>
        hostname == "localhost" || hostname.EndsWith(".localhost", StringComparison.Ordinal);
}

public static class BrowserApplicationTenants
{
    public const string PatientsAppId = "vitarish-patients";
    public const string ProjectManagementAppId = "vitarish-project-management";
    public const string KnowledgeQaAppId = "vitarish-knowledge-qa";
    public const string PatientsName = "Vitarish Patients";
    public const string ProjectManagementName = "Vitarish Project Management";
    public const string KnowledgeQaName = "Vitarish Knowledge Demo";
    /// <summary>The clinic domain's own host, where the owner landing page and its sign-in live; it holds no practice data.</summary>
    public const string ClinicLandingAppId = "vitarish-clinic-landing";
    public const string ClinicLandingName = "Vitarish Clinic Landing";
}
