using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Main.Tests;

public sealed class InternalModuleApiArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void InternalModuleApiMappingsUseProtectedEndpointGroups()
    {
        string[] allowedFiles =
        [
            Normalize("src/Modules/Base/Endpoints/ProtectedModuleEndpointRouteBuilderExtensions.cs"),
            Normalize("src/Modules/Social/Module/Endpoints/SocialWebhookEndpointRouteBuilderExtensions.cs")
        ];

        string[] files = Directory.GetFiles(Path.Combine(RepositoryRoot, "src", "Modules"), "*.cs", SearchOption.AllDirectories)
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}Api{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}Module{Path.DirectorySeparatorChar}Endpoints{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        List<string> failures = [];
        foreach (string file in files)
        {
            string relative = Normalize(Path.GetRelativePath(RepositoryRoot, file));
            string text = File.ReadAllText(file);
            if (allowedFiles.Contains(relative)) continue;

            foreach (Match match in Regex.Matches(text, @"\bendpoints\.Map(Group|Get|Post|Put|Delete|Patch)\s*\("))
            {
                failures.Add($"{relative}: bypasses protected module endpoint mapping at {match.Value}");
            }
        }

        failures.Should().BeEmpty("internal module API endpoints must use MapProtectedModuleGroup, and anonymous routes must use MapAnonymousModuleGroup with a justification");
    }

    [Fact]
    public void InternalModuleClientsUseAuthenticatedTransportAndSharedRegistration()
    {
        string[] clientFiles = Directory.GetFiles(Path.Combine(RepositoryRoot, "src", "Modules"), "*Client.cs", SearchOption.AllDirectories)
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}Client{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        List<string> failures = [];
        foreach (string file in clientFiles)
        {
            string relative = Normalize(Path.GetRelativePath(RepositoryRoot, file));
            string text = File.ReadAllText(file);
            if (text.Contains("HttpClient", StringComparison.Ordinal) && !text.Contains("AuthenticatedModuleApiClient", StringComparison.Ordinal))
            {
                failures.Add($"{relative}: client injects or uses raw HttpClient without inheriting AuthenticatedModuleApiClient.");
            }
        }

        string[] interfaceFiles = Directory.GetFiles(Path.Combine(RepositoryRoot, "src", "Modules"), "*Extensions.cs", SearchOption.AllDirectories)
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}Interface{Path.DirectorySeparatorChar}DependencyInjection{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        foreach (string file in interfaceFiles)
        {
            string relative = Normalize(Path.GetRelativePath(RepositoryRoot, file));
            string text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(text, @"AddScoped\s*<\s*I\w+Client\s*,\s*\w+Client\s*>"))
            {
                failures.Add($"{relative}: direct module client registration {match.Value}; use AddInternalModuleApiClient.");
            }
        }

        failures.Should().BeEmpty("internal module clients must share authenticated HTTP behavior and registration");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vitarish.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
