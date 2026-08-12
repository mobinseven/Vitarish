using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

internal static class InternalModuleClientCheck
{
    private static readonly HashSet<string> RegistrationMethods =
        ["AddHttpClient", "AddScoped", "AddTransient", "AddSingleton"];

    private const string BaseRegistration = "src/Modules/Base/Client/DependencyInjection/InternalModuleClientRegistrationExtensions.cs";

    public static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        HashSet<string> clientClassNames = new(StringComparer.Ordinal);
        IEnumerable<SourceFile> clientFiles = index.Find(path =>
            path.StartsWith("src/Modules/", StringComparison.OrdinalIgnoreCase) &&
            path.Contains("/Client/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("src/Modules/Base/Client/", StringComparison.OrdinalIgnoreCase));

        foreach (SourceFile file in clientFiles)
        {
            foreach (ClassDeclarationSyntax declaration in file.Tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string className = declaration.Identifier.ValueText;
                clientClassNames.Add(className);

                SyntaxNode? httpClientNode = declaration.DescendantNodes()
                    .FirstOrDefault(node => node is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "HttpClient");
                if (httpClientNode is not null && !IsAuthenticatedClient(declaration, file.Model))
                {
                    violations.Add(new(file.RelativePath, RoslynSyntax.Line(httpClientNode),
                        "UnauthenticatedModuleClient", "HttpClient",
                        "Internal module clients must derive from AuthenticatedModuleApiClient."));
                }
            }
        }

        foreach (SourceFile file in index.Files)
        {
            if (file.RelativePath.Equals(BaseRegistration, StringComparison.OrdinalIgnoreCase)) continue;

            foreach (InvocationExpressionSyntax invocation in file.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string method = RoslynSyntax.Name(invocation);
                if (!RegistrationMethods.Contains(method) || invocation.Expression is not MemberAccessExpressionSyntax member ||
                    member.Name is not GenericNameSyntax generic)
                {
                    continue;
                }

                IEnumerable<string> registeredTypes = generic.TypeArgumentList.Arguments
                    .Select(type => RoslynSyntax.TypeName(type))
                    .Where(type => type is not null)
                    .Select(type => type!);
                string? registeredClient = registeredTypes.FirstOrDefault(clientClassNames.Contains);
                if (registeredClient is not null)
                {
                    violations.Add(new(file.RelativePath, RoslynSyntax.Line(invocation),
                        "DirectModuleClientRegistration", method,
                        "Register internal module clients through AddInternalModuleApiClient<TClientContract,TClientImplementation>."));
                }
            }
        }

        return violations;
    }

    private static bool IsAuthenticatedClient(ClassDeclarationSyntax declaration, SemanticModel model)
    {
        INamedTypeSymbol? symbol = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
        for (INamedTypeSymbol? current = symbol?.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "AuthenticatedModuleApiClient" ||
                current.ToDisplayString() == "Vitarish.Modules.Base.Client.AuthenticatedModuleApiClient")
            {
                return true;
            }
        }

        return declaration.BaseList?.Types.Any(type => type.ToString().Contains("AuthenticatedModuleApiClient", StringComparison.Ordinal)) == true;
    }
}
