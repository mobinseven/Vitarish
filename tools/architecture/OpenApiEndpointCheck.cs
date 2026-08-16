using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

internal static class OpenApiEndpointCheck
{
    private static readonly HashSet<string> RawMappings =
        ["MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods", "MapGroup"];

    private static readonly HashSet<string> NoBodyStatuses =
        ["StatusCodes.Status202Accepted", "StatusCodes.Status204NoContent", "202", "204"];

    private static readonly Dictionary<string, string> DefaultStatuses = new(StringComparer.Ordinal)
    {
        ["MapModuleGet"] = "200", ["MapModulePost"] = "200", ["MapModulePut"] = "200",
        ["MapModulePatch"] = "200", ["MapModuleHead"] = "200", ["MapModuleDelete"] = "204"
    };

    private static readonly HashSet<string> PrimitiveTypes =
    [
        "String", "string", "Guid", "Boolean", "bool", "Int32", "int", "Int64", "long",
        "Decimal", "decimal", "Double", "double", "Single", "float", "DateTime", "DateOnly",
        "TimeOnly", "Stream", "IResult", "Results", "IFormFile", "CancellationToken"
    ];

    private const string AllowedRawFile = "src/Modules/Base/Core/Endpoints/ModuleEndpointMappingExtensions.cs";
    private const string AllowedProtectedRawFile = "src/Modules/Base/Core/Endpoints/ProtectedModuleEndpointRouteBuilderExtensions.cs";

    public static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        foreach (SourceFile file in Files(index))
        {
            if (file.RelativePath is AllowedRawFile or AllowedProtectedRawFile) continue;
            violations.AddRange(ScanFile(index, file));
        }

        return violations;
    }

    private static IEnumerable<SourceFile> Files(SourceIndex index)
    {
        HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase) { "src/Hosts/Main/Program.cs" };
        foreach (SourceFile file in index.Files)
        {
            string path = file.RelativePath;
            if (path.StartsWith("src/Interfaces/", StringComparison.OrdinalIgnoreCase) ||
                (path.StartsWith("src/Modules/", StringComparison.OrdinalIgnoreCase) &&
                 (path.Contains("/Api/", StringComparison.OrdinalIgnoreCase) ||
                  path.Contains("/Module/Endpoints/", StringComparison.OrdinalIgnoreCase) ||
                  (path.Contains("/Infrastructure/Extensions/", StringComparison.OrdinalIgnoreCase) &&
                   Path.GetFileName(path).Contains("Endpoint", StringComparison.OrdinalIgnoreCase)))))
            {
                selected.Add(path);
            }
        }

        return index.Files.Where(file => selected.Contains(file.RelativePath));
    }

    private static IReadOnlyList<ArchitectureViolation> ScanFile(SourceIndex index, SourceFile file)
    {
        SyntaxNode root = file.Tree.GetRoot();
        List<ArchitectureViolation> violations = [];
        HashSet<string> excludedGroups = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => RoslynSyntax.Name(invocation) == "ExcludeFromOpenApiWithJustification")
            .Select(invocation => (invocation.Expression as MemberAccessExpressionSyntax)?.Expression.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            string method = RoslynSyntax.Name(invocation);
            if (method == "MapModuleEndpoints") continue;
            if (RawMappings.Contains(method) && !IsExcluded(invocation, excludedGroups))
            {
                violations.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "RawEndpointMapping", method,
                    "Use MapProtectedModuleGroup/MapAnonymousModuleGroup and MapModule* helpers, or ExcludeFromOpenApiWithJustification for non-module endpoints."));
            }

            if (method == "MapAnonymousModuleGroup" &&
                (invocation.ArgumentList.Arguments.Count < 2 ||
                 !RoslynSyntax.IsStringLiteral(invocation.ArgumentList.Arguments[1].Expression.ToString(), out string justification) ||
                 string.IsNullOrWhiteSpace(justification)))
            {
                violations.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "AnonymousJustificationMissing", method,
                    "Provide a non-empty machine-readable justification."));
            }

            if (method.StartsWith("MapModule", StringComparison.Ordinal))
            {
                violations.AddRange(ScanModuleCall(index, file, invocation, method));
            }
        }

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                     .Where(invocation => RoslynSyntax.Name(invocation) == "ExcludeFromDescription"))
        {
            string nearby = file.Text[Math.Max(0, invocation.SpanStart - 200)..Math.Min(file.Text.Length, invocation.Span.End + 200)];
            if (!nearby.Contains("ExcludeFromOpenApiWithJustification", StringComparison.Ordinal))
            {
                violations.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "OpenApiExclusionJustificationMissing",
                    "ExcludeFromDescription", "Use ExcludeFromOpenApiWithJustification with typed metadata; raw string metadata is not accepted."));
            }
        }

        foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()
                     .Where(identifier => identifier.Identifier.ValueText == "OpenApiExclusionJustification"))
        {
            violations.Add(new(file.RelativePath, RoslynSyntax.Line(identifier), "RawOpenApiExclusionMetadata",
                "OpenApiExclusionJustification", "Use ExcludeFromOpenApiWithJustification; raw string metadata is not accepted."));
        }

        return violations;
    }

    private static bool IsExcluded(InvocationExpressionSyntax invocation, HashSet<string> excludedGroups)
    {
        ExpressionStatementSyntax? statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement is not null && statement.ToString().Contains("ExcludeFromOpenApiWithJustification", StringComparison.Ordinal))
        {
            return true;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax member && excludedGroups.Contains(member.Expression.ToString()))
        {
            return true;
        }

        string? assignedVariable = AssignedVariableName(invocation);
        return assignedVariable is not null && excludedGroups.Contains(assignedVariable);
    }

    private static string? AssignedVariableName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Parent is EqualsValueClauseSyntax initializer &&
            initializer.Parent is VariableDeclaratorSyntax declarator)
        {
            return declarator.Identifier.ValueText;
        }

        if (invocation.Parent is AssignmentExpressionSyntax assignment &&
            assignment.Left is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        return null;
    }

    private static IEnumerable<ArchitectureViolation> ScanModuleCall(
        SourceIndex index,
        SourceFile file,
        InvocationExpressionSyntax invocation,
        string method)
    {
        List<ArchitectureViolation> violations = [];
        int line = RoslynSyntax.Line(invocation);
        string? operationId = RoslynSyntax.ArgumentExpression(invocation, "operationId", 2);
        if (!RoslynSyntax.IsStringLiteral(operationId, out string operation) || !Regex.IsMatch(operation, "^[A-Z][A-Za-z0-9]+(?:_[A-Za-z0-9]+)*$"))
        {
            violations.Add(new(file.RelativePath, line, "OperationIdMissing", method,
                "Pass a stable PascalCase operation ID."));
        }

        string? successType = RoslynSyntax.ArgumentExpression(invocation, "successType", 3);
        string? status = RoslynSyntax.ArgumentExpression(invocation, "successStatusCode", -1);
        string? profile = RoslynSyntax.ArgumentExpression(invocation, "profile", -1);
        string? contentType = RoslynSyntax.ArgumentExpression(invocation, "contentType", -1);
        string? anonymousJustification = RoslynSyntax.ArgumentExpression(invocation, "anonymousJustification", -1);
        string statusToken = status?.Trim() ?? DefaultStatuses.GetValueOrDefault(method, "200");

        if (successType is null && !NoBodyStatuses.Contains(statusToken) &&
            profile is not null && (profile.Contains("MultipartUpload", StringComparison.Ordinal) || profile.Contains("LongRunningCommand", StringComparison.Ordinal)))
        {
            violations.Add(new(file.RelativePath, line, "SuccessMetadataMissing", method,
                "Provide successType: typeof(ResponseContract) or explicit no-body successStatusCode such as StatusCodes.Status204NoContent."));
        }

        if (status is null && (method is "MapModulePost" or "MapModulePut" or "MapModulePatch") &&
            profile?.Contains("LongRunningCommand", StringComparison.Ordinal) == true)
        {
            violations.Add(new(file.RelativePath, line, "SuccessStatusCodeImplicit", method,
                "Provide explicit successStatusCode when the helper default is not semantically correct."));
        }

        if (profile is not null && (profile.Contains("BinaryDownload", StringComparison.Ordinal) || profile.Contains("Streaming", StringComparison.Ordinal)) && contentType is null)
        {
            violations.Add(new(file.RelativePath, line, "ProfileContentTypeMissing", method,
                "BinaryDownload and Streaming endpoints must declare contentType."));
        }

        if (profile?.Contains("AnonymousWebhook", StringComparison.Ordinal) == true && anonymousJustification is null)
        {
            violations.Add(new(file.RelativePath, line, "AnonymousJustificationMissing", method,
                "Anonymous endpoints must provide anonymousJustification."));
        }

        ArgumentSyntax? successArgument = FindArgument(invocation, "successType", 3);
        ITypeSymbol? successSymbol = successArgument?.Expression is TypeOfExpressionSyntax typeOf
            ? file.Model.GetTypeInfo(typeOf.Type).Type
            : null;
        if (!SchemaIsOwned(index, file, successType, successSymbol, out string successReason))
        {
            violations.Add(new(file.RelativePath, line, "SchemaOwnershipViolation", TypeToken(successType) ?? method, successReason));
        }

        string? handler = RoslynSyntax.ArgumentExpression(invocation, "handler", 1);
        foreach (IParameterSymbol parameter in FindHandler(index, file, invocation, handler)?.Parameters ?? [])
        {
            if (!IsRequestParameter(parameter)) continue;
            if (!SchemaIsOwned(index, file, parameter.Type.ToDisplayString(), parameter.Type, out string reason))
            {
                violations.Add(new(file.RelativePath, line, "RequestSchemaOwnershipViolation", parameter.Type.Name, reason));
            }
        }

        return violations;
    }

    private static ArgumentSyntax? FindArgument(InvocationExpressionSyntax invocation, string name, int position)
    {
        ArgumentSyntax? named = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
            string.Equals(argument.NameColon?.Name.Identifier.ValueText, name, StringComparison.Ordinal));
        return named ?? (position < 0
            ? null
            : invocation.ArgumentList.Arguments.Where(argument => argument.NameColon is null).ElementAtOrDefault(position));
    }

    private static IMethodSymbol? FindHandler(SourceIndex index, SourceFile file, InvocationExpressionSyntax invocation, string? handler)
    {
        if (handler is null || handler.Contains("=>", StringComparison.Ordinal) || handler.StartsWith("(", StringComparison.Ordinal)) return null;
        ArgumentSyntax? argument = invocation.ArgumentList.Arguments
            .Where(item => item.NameColon is null)
            .ElementAtOrDefault(1);
        if (argument is not null && file.Model.GetSymbolInfo(argument.Expression).Symbol is IMethodSymbol method)
        {
            return method;
        }

        string name = handler.Split('.').Last().Trim();
        IEnumerable<SourceFile> candidates = [file, .. index.Files.Where(source => source != file)];
        return candidates.SelectMany(source => source.Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(method => method.Identifier.ValueText == name)
            .Select(method => candidates.First(source => source.Tree == method.SyntaxTree).Model.GetDeclaredSymbol(method) as IMethodSymbol)
            .FirstOrDefault(symbol => symbol is not null);
    }

    private static bool IsRequestParameter(IParameterSymbol parameter)
    {
        if (parameter.Type.Name.StartsWith("I", StringComparison.Ordinal) && parameter.Type.Name.Length > 1 && char.IsUpper(parameter.Type.Name[1])) return false;
        if (parameter.Type.Name.EndsWith("DbContext", StringComparison.Ordinal) ||
            parameter.Type.Name is "ClaimsPrincipal" or "CancellationToken" ||
            parameter.Type.Name.StartsWith("Http", StringComparison.Ordinal) ||
            parameter.Type.Name.StartsWith("SignInManager", StringComparison.Ordinal) ||
            parameter.Type.Name.StartsWith("UserManager", StringComparison.Ordinal)) return false;

        bool hasBindingAttribute = parameter.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name is "AsParametersAttribute" or "FromBodyAttribute");
        return hasBindingAttribute || parameter.Type.Name.EndsWith("Request", StringComparison.Ordinal) ||
               parameter.Type.Name.EndsWith("Parameters", StringComparison.Ordinal) ||
               parameter.Type.Name.EndsWith("Dto", StringComparison.Ordinal);
    }

    private static bool SchemaIsOwned(SourceIndex index, SourceFile file, string? expression, ITypeSymbol? resolvedType, out string reason)
    {
        if (expression is "EntityResult" or "Vitarish.Entities.Contracts.Models.EntityResult")
        {
            reason = "legacy compatibility contract";
            return true;
        }

        if (string.IsNullOrWhiteSpace(expression))
        {
            reason = "no schema";
            return true;
        }

        string token = TypeToken(expression) ?? expression;
        if (PrimitiveTypes.Contains(token.Split('.').Last().TrimEnd('?').TrimEnd('[', ']')))
        {
            reason = "primitive";
            return true;
        }

        ITypeSymbol? schema = resolvedType switch
        {
            IArrayTypeSymbol array => array.ElementType,
            INamedTypeSymbol named when named.IsGenericType && named.TypeArguments.Length == 1 &&
                                        named.Name is "Task" or "ValueTask" or "IEnumerable" or "IReadOnlyList" or "List" or "Collection" => named.TypeArguments[0],
            _ => resolvedType
        };

        if (schema?.Name == "EntityResult" &&
            schema.ContainingNamespace.ToDisplayString() == "Vitarish.Entities.Contracts.Models")
        {
            reason = "legacy compatibility contract";
            return true;
        }

        if (schema is not null && PrimitiveTypes.Contains(schema.Name))
        {
            reason = "primitive";
            return true;
        }

        if (schema is not null && schema is not IErrorTypeSymbol)
        {
            Location? location = schema.Locations.FirstOrDefault(item => item.IsInSource);
            string? path = location?.SourceTree?.FilePath is null ? null : Path.GetRelativePath(index.Root, location.SourceTree.FilePath).Replace(Path.DirectorySeparatorChar, '/');
            if (path?.StartsWith("src/Modules/", StringComparison.OrdinalIgnoreCase) == true &&
                path.Contains("/Contracts/", StringComparison.OrdinalIgnoreCase))
            {
                reason = path;
                return true;
            }

            reason = $"{schema.Name} resolves to {path ?? schema.ToDisplayString()}; OpenAPI schemas must be module Contracts shapes.";
            return false;
        }

        if (expression.Contains(".Contracts.", StringComparison.Ordinal))
        {
            reason = "contract namespace";
            return true;
        }

        reason = $"Could not resolve schema type {token}; use an explicit module Contracts DTO.";
        return false;
    }

    private static string? TypeToken(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        if (expression.StartsWith("typeof", StringComparison.Ordinal))
        {
            int open = expression.IndexOf('(');
            int close = expression.LastIndexOf(')');
            return open >= 0 && close > open ? expression[(open + 1)..close].Trim() : null;
        }

        return expression.Trim();
    }
}
