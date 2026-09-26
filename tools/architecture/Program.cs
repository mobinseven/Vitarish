using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

internal static class Program
{
    private static int Main(string[] args)
    {
        string root = FindRepositoryRoot();
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase) ||
            args.Contains("self-test", StringComparer.OrdinalIgnoreCase))
        {
            SelfTests.Run(root);
            return 0;
        }

        string command = args.FirstOrDefault() ?? "all";
        SourceIndex index = SourceIndex.Load(root);

        if (command == "authorization-matrix-inventory")
        {
            Console.WriteLine(AuthorizationMatrixCheck.RenderInventory(index));
            return 0;
        }

        if (command == "dependency-vulnerability-baseline")
            return DependencyVulnerabilityBaselineCheck.Run(root);

        IReadOnlyList<ArchitectureViolation> violations = command switch
        {
            "all" => RunAll(index),
            "app-experience" => AppExperienceBoundaryCheck.Run(root),
            "internal-module-clients" => InternalModuleClientCheck.Run(index),
            "openapi-endpoints" => OpenApiEndpointCheck.Run(index),
            "security-baseline" => SecurityBaselineCheck.Run(index),
            "agents" => AgentArchitectureCheck.Run(root),
            _ => throw new ArgumentException($"Unknown architecture check '{command}'.")
        };

        string title = command switch
        {
            "app-experience" => "ADR-0008 app experience validation",
            "internal-module-clients" => "Internal module client boundary validation",
            "openapi-endpoints" => "OpenAPI endpoint coverage validation",
            "security-baseline" => "Source-derived security baseline validation",
            "agents" => "Module-owned Agents architecture validation",
            _ => "Architecture validation"
        };

        Console.WriteLine(title);
        if (command == "security-baseline")
            SecurityBaselineCheck.RenderInventory(index);

        Console.WriteLine($"Violations found: {violations.Count}");
        foreach (ArchitectureViolation violation in violations)
        {
            Console.WriteLine($"{violation.Path}:{violation.Line}: kind={violation.Kind}; " +
                              $"endpoint={violation.Expression}; remediation={violation.Remediation}");
        }

        return violations.Count == 0 ? 0 : 1;
    }

    private static IReadOnlyList<ArchitectureViolation> RunAll(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        violations.AddRange(AppExperienceBoundaryCheck.Run(index.Root));
        violations.AddRange(InternalModuleClientCheck.Run(index));
        violations.AddRange(OpenApiEndpointCheck.Run(index));
        violations.AddRange(AgentArchitectureCheck.Run(index.Root));
        return violations;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vitarish.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}

internal sealed record ArchitectureViolation(
    string Path,
    int Line,
    string Kind,
    string Expression,
    string Remediation);

internal sealed record SourceFile(
    string RelativePath,
    string Text,
    SyntaxTree Tree,
    SemanticModel Model);

internal sealed class SourceIndex
{
    private SourceIndex(string root, CSharpCompilation compilation, IReadOnlyList<SourceFile> files)
    {
        Root = root;
        Compilation = compilation;
        Files = files;
    }

    public string Root { get; }
    public CSharpCompilation Compilation { get; }
    public IReadOnlyList<SourceFile> Files { get; }

    public IEnumerable<SourceFile> Find(Func<string, bool> predicate) => Files.Where(file => predicate(file.RelativePath));

    public static SourceIndex Load(string root)
    {
        IEnumerable<string> paths = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Intersect(["bin", "obj"], StringComparer.OrdinalIgnoreCase).Any())
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        return Create(root, paths.Select(path =>
        {
            string relativePath = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            return (relativePath, File.ReadAllText(path));
        }));
    }

    public static SourceIndex FromSources(params (string RelativePath, string Text)[] sources) =>
        Create(Directory.GetCurrentDirectory(), sources);

    private static SourceIndex Create(string root, IEnumerable<(string RelativePath, string Text)> sources)
    {
        List<(string RelativePath, string Text, SyntaxTree Tree)> parsed = sources
            .Select(source =>
            {
                string path = Path.Combine(root, source.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                SyntaxTree tree = CSharpSyntaxTree.ParseText(source.Text, new CSharpParseOptions(LanguageVersion.Latest), path);
                return (source.RelativePath.Replace(Path.DirectorySeparatorChar, '/'), source.Text, tree);
            })
            .ToList();

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Vitarish.Architecture.SourceAnalysis",
            syntaxTrees: parsed.Select(item => item.Tree),
            references: RuntimeReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IReadOnlyList<SourceFile> files = parsed
            .Select(item => new SourceFile(item.RelativePath, item.Text, item.Tree, compilation.GetSemanticModel(item.Tree)))
            .ToList();

        return new SourceIndex(root, compilation, files);
    }

    private static IEnumerable<MetadataReference> RuntimeReferences()
    {
        string? trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        return trustedAssemblies is null
            ? [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
            : trustedAssemblies.Split(Path.PathSeparator).Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path));
    }
}

internal static class RoslynSyntax
{
    public static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    public static string Name(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => string.Empty
    };

    public static string? TypeName(TypeSyntax? type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        QualifiedNameSyntax qualified => TypeName(qualified.Right),
        AliasQualifiedNameSyntax alias => TypeName(alias.Name),
        GenericNameSyntax generic => generic.Identifier.ValueText,
        NullableTypeSyntax nullable => TypeName(nullable.ElementType),
        ArrayTypeSyntax array => TypeName(array.ElementType),
        _ => type?.ToString()
    };

    public static string? ArgumentExpression(InvocationExpressionSyntax invocation, string name, int position)
    {
        ArgumentSyntax? named = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
            string.Equals(argument.NameColon?.Name.Identifier.ValueText, name, StringComparison.Ordinal));
        if (named is not null) return named.Expression.ToString();

        ArgumentSyntax? positional = position < 0
            ? null
            : invocation.ArgumentList.Arguments.Where(argument => argument.NameColon is null).ElementAtOrDefault(position);
        return positional?.Expression.ToString();
    }

    public static bool IsStringLiteral(string? expression, out string value)
    {
        if (expression is not null && expression.Length >= 2 && expression[0] == '"' && expression[^1] == '"')
        {
            value = expression[1..^1];
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static string? ResolveString(SourceFile file, ExpressionSyntax? expression)
    {
        if (expression is null)
            return null;
        if (expression.ToString() == "string.Empty")
            return string.Empty;

        Optional<object?> constant = file.Model.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is string value)
            return value;

        if (expression is InterpolatedStringExpressionSyntax interpolated &&
            interpolated.Contents.All(content => content is InterpolatedStringTextSyntax))
            return string.Concat(interpolated.Contents.OfType<InterpolatedStringTextSyntax>().Select(content => content.TextToken.ValueText));

        if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
        {
            string? left = ResolveString(file, binary.Left);
            string? right = ResolveString(file, binary.Right);
            return left is null || right is null ? null : left + right;
        }

        return null;
    }

    public static bool IsUnder(SourceFile file, string folder) =>
        file.RelativePath.StartsWith(folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
}
