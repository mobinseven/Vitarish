namespace Vitarish.Tools.Architecture;

internal static class AppExperienceBoundaryCheck
{
    private static readonly HashSet<string> Collections =
    [
        "decision-trees", "journeys", "tests", "decisions", "roadmaps", "tasks", "runbooks"
    ];

    public static IReadOnlyList<ArchitectureViolation> Run(string root)
    {
        string docsApps = Path.Combine(root, "docs", "apps");
        if (!Directory.Exists(docsApps)) return [];

        List<ArchitectureViolation> violations = [];
        foreach (string path in Directory.EnumerateFiles(docsApps, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            string appRelative = Path.GetRelativePath(docsApps, path).Replace(Path.DirectorySeparatorChar, '/');
            string[] parts = appRelative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            string appName = parts[0];
            string collection = parts[1];
            bool allowedLegacyModulePath = appRelative.Equals("clinic-owner/modules", StringComparison.OrdinalIgnoreCase) ||
                                           appRelative.StartsWith("clinic-owner/modules/", StringComparison.OrdinalIgnoreCase);

            if (collection.Equals("modules", StringComparison.OrdinalIgnoreCase) && !allowedLegacyModulePath)
            {
                violations.Add(new(relative, 1, "LegacyModulesPath", relative,
                    $"ADR-0008 app docs must use flat app-owned collections."));
            }

            if (Collections.Contains(collection) && parts.Length > 3)
            {
                violations.Add(new(relative, 1, "NestedCollection", relative,
                    $"Collection files must be flat below docs/apps/{appName}/{collection}."));
            }

            string text = File.ReadAllText(path);
            if (collection.Equals("tests", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("journeys/", StringComparison.Ordinal) &&
                !text.Contains("../journeys/", StringComparison.Ordinal) &&
                !text.Contains("Related journey: N/A", StringComparison.Ordinal))
            {
                violations.Add(new(relative, 1, "TestJourneyReference", relative,
                    "App test docs must cite an app journey or N/A."));
            }

            if (collection.Equals("journeys", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("Derives from:", StringComparison.Ordinal) &&
                !text.Contains("decision-trees/", StringComparison.Ordinal))
            {
                violations.Add(new(relative, 1, "JourneyDecisionTreeReference", relative,
                    "Journeys that derive from an upstream document must link to an app decision tree."));
            }
        }

        return violations;
    }
}
