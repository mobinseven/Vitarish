using System.Xml.Linq;

namespace Vitarish.Tools.Architecture;

internal static class AgentArchitectureCheck
{
    private static readonly string[] ForbiddenReferenceTokens = ["Infrastructure", "Api", "Client", "Contracts", "Module", "Host", "EntityFramework", "OpenAI", "Anthropic", "Ollama"];

    public static IReadOnlyList<ArchitectureViolation> Run(string root)
    {
        List<ArchitectureViolation> violations = [];
        string sourceRoot = Path.Combine(root, "src");
        foreach (string projectPath in Directory.EnumerateFiles(sourceRoot, "*.Agents.csproj", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, projectPath).Replace('\\', '/');
            XElement project = XDocument.Load(projectPath).Root!;
            List<string> references = project.Descendants("ProjectReference")
                .Select(x => Path.GetFileNameWithoutExtension((string?)x.Attribute("Include") ?? string.Empty))
                .Where(x => x.Length > 0).ToList();
            if (project.Descendants("PackageReference").Any())
                violations.Add(new(relative, 1, "agents-package-reference", relative, "Agents projects must remain provider-neutral and package-free."));

            bool isIntelligence = relative.EndsWith("/Modules/Intelligence/Agents/Intelligence.Agents.csproj", StringComparison.OrdinalIgnoreCase);
            foreach (string reference in references)
            {
                bool allowed = isIntelligence
                    ? reference is "Intelligence.Application"
                    : reference.EndsWith(".Application", StringComparison.Ordinal) || reference == "Intelligence.Agents" || reference.EndsWith(".Agents", StringComparison.Ordinal);
                if (!allowed || ForbiddenReferenceTokens.Any(token => reference.Contains(token, StringComparison.OrdinalIgnoreCase) && !reference.EndsWith(".Application", StringComparison.Ordinal) && reference != "Intelligence.Agents"))
                    violations.Add(new(relative, 1, "agents-project-reference", reference, "Reference only the owning Application, Intelligence.Agents, explicitly selected Agents projects, and approved shared libraries."));
            }

            string projectDirectory = Path.GetDirectoryName(projectPath)!;
            foreach (string sourcePath in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(sourcePath);
                if (text.Contains("Assembly.Load", StringComparison.Ordinal) || text.Contains("GetTypes()", StringComparison.Ordinal) || text.Contains("BuildServiceProvider", StringComparison.Ordinal))
                    violations.Add(new(Path.GetRelativePath(root, sourcePath).Replace('\\', '/'), 1, "agents-dynamic-discovery", "reflection/service-provider activation", "Use explicit registration and statically analyzable serialization."));
            }
        }
        return violations;
    }
}
