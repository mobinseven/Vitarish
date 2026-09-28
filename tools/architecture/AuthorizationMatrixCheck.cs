using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

internal static partial class AuthorizationMatrixCheck
{
    private const string MatrixPath = "docs/security/authorization-matrix.md";
    private const string StartMarker = "<!-- main-module-authorization-matrix:start -->";
    private const string EndMarker = "<!-- main-module-authorization-matrix:end -->";
    private static readonly HashSet<string> ModuleMappings =
        ["MapModuleGet", "MapModulePost", "MapModulePut", "MapModulePatch", "MapModuleDelete", "MapModuleHead"];
    private static readonly HashSet<string> ReauthenticationValues = ["Required", "Not required", "Conditional"];

    public static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index) =>
        Run(index, File.Exists(Path.Combine(index.Root, MatrixPath))
            ? File.ReadAllText(Path.Combine(index.Root, MatrixPath))
            : string.Empty);

    internal static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index, string matrixText)
    {
        List<ArchitectureViolation> violations = [];
        IReadOnlyList<ModuleOperation> operations = ExtractOperations(index);
        IReadOnlyList<MatrixRow> rows = ParseRows(matrixText, violations);

        foreach (IGrouping<string, MatrixRow> duplicate in rows.GroupBy(row => row.OperationId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            violations.Add(new(MatrixPath, duplicate.First().Line, "DuplicateAuthorizationMatrixOperation", duplicate.Key,
                "Keep exactly one matrix row for each stable operation ID."));
        }

        Dictionary<string, MatrixRow> uniqueRows = rows
            .GroupBy(row => row.OperationId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        Dictionary<string, ModuleOperation> sourceOperations = operations
            .GroupBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (IGrouping<string, ModuleOperation> duplicate in operations.GroupBy(operation => operation.OperationId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            ModuleOperation first = duplicate.First();
            violations.Add(new(first.RouteOwner, first.Line, "DuplicateOperationId", duplicate.Key,
                "Operation IDs are compatibility identifiers and must be unique across Main module endpoints."));
        }

        foreach (ModuleOperation operation in operations.Where(operation => !uniqueRows.ContainsKey(operation.OperationId)))
        {
            violations.Add(new(operation.RouteOwner, operation.Line, "MissingAuthorizationMatrixOperation", operation.OperationId,
                $"Add a checked row to {MatrixPath} for this Main-composed module operation."));
        }

        foreach (MatrixRow row in rows.Where(row => !sourceOperations.ContainsKey(row.OperationId)))
        {
            violations.Add(new(MatrixPath, row.Line, "StaleAuthorizationMatrixOperation", row.OperationId,
                "Remove the stale row or restore the endpoint mapping; endpoint mapping remains executable truth."));
        }

        foreach (ModuleOperation operation in operations)
        {
            if (!uniqueRows.TryGetValue(operation.OperationId, out MatrixRow? row)) continue;
            ValidateRow(index, operation, row, violations);
        }

        return violations;
    }

    internal static IReadOnlyList<ModuleOperation> ExtractOperations(SourceIndex index)
    {
        List<ModuleOperation> operations = [];
        foreach (SourceFile file in OpenApiEndpointCheck.Files(index).Where(file => file.RelativePath.StartsWith("src/Modules/", StringComparison.OrdinalIgnoreCase)))
        {
            string[] pathParts = file.RelativePath.Split('/');
            if (pathParts.Length < 3 || string.Equals(pathParts[2], "Base", StringComparison.OrdinalIgnoreCase)) continue;
            string module = pathParts[2];

            foreach (InvocationExpressionSyntax invocation in file.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string method = RoslynSyntax.Name(invocation);
                if (!ModuleMappings.Contains(method)) continue;
                ArgumentSyntax? operationArgument = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
                    string.Equals(argument.NameColon?.Name.Identifier.ValueText, "operationId", StringComparison.Ordinal)) ??
                    invocation.ArgumentList.Arguments.Where(argument => argument.NameColon is null).ElementAtOrDefault(2);
                string? operationId = RoslynSyntax.ResolveString(file, operationArgument?.Expression);
                if (operationId is null) continue;

                ExpressionStatementSyntax? statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                string statementText = statement?.ToString() ?? invocation.ToString();
                if (statementText.Contains("ExcludeFromOpenApiWithJustification", StringComparison.Ordinal)) continue;

                string? profile = RoslynSyntax.ArgumentExpression(invocation, "profile", -1);
                bool anonymous = profile?.Contains("Anonymous", StringComparison.Ordinal) == true;
                string classification = anonymous ? "anonymous" : "protected";
                string authentication = anonymous
                    ? "None"
                    : module == "Platform"
                        ? "BrowserOrBearer + Policy:\"PlatformAdmin\""
                        : ExpectedAuthentication(statementText);
                string rationale = anonymous
                    ? RoslynSyntax.ResolveString(file, invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
                        string.Equals(argument.NameColon?.Name.Identifier.ValueText, "anonymousJustification", StringComparison.Ordinal))?.Expression) ?? string.Empty
                    : "—";
                operations.Add(new(operationId, module, file.RelativePath, RoslynSyntax.Line(invocation), classification, authentication, rationale));
            }
        }

        return operations.OrderBy(operation => operation.Module, StringComparer.Ordinal)
            .ThenBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static string RenderInventory(SourceIndex index)
    {
        IEnumerable<string> rows = ExtractOperations(index).Select(operation =>
            $"| `{operation.OperationId}` | {operation.Module} | `{operation.RouteOwner}` | {operation.Classification} | `{operation.Authentication}` | {DefaultAuthority(operation)} | {DefaultReauthentication(operation)} | {operation.PublicRationale} | `{DefaultRegressionTest(operation)}` |");
        return string.Join(Environment.NewLine, rows);
    }

    private static string DefaultAuthority(ModuleOperation operation)
    {
        if (operation.Classification == "anonymous")
            return "Public transport only; owning Application validates credentials, provider proof, request limits, and operation preconditions";
        if (operation.Authentication.Contains("PlatformAdmin", StringComparison.Ordinal))
            return "Platform administrator policy at transport; owning Application validates target/resource identifiers";
        if (operation.Module == "Authentication")
            return "Authenticated account subject; Authentication Application validates the account workflow";
        if (operation.Module == "Requests")
            return "Authenticated request subject; Requests owns resolved request-context data";
        return "Authenticated principal; owning Application use case owns tenant, subject, resource, and role checks";
    }

    private static string DefaultReauthentication(ModuleOperation operation) =>
        operation.OperationId.Contains("Account_", StringComparison.Ordinal) &&
        operation.OperationId.Contains("Change", StringComparison.OrdinalIgnoreCase)
            ? "Required"
            : "Not required";

    private static string DefaultRegressionTest(ModuleOperation operation) => operation.Module switch
    {
        "Authentication" => "tests/Modules/Authentication.Tests/Api/ProtectedAuthenticationEndpointAuthorizationTests.cs#ProtectedAuthenticationEndpointAuthorizationTests",
        "Intelligence" => "tests/Modules/Intelligence.Tests/Api/RealtimeTranscriptionBoundaryTests.cs#Capability_and_websocket_endpoints_are_protected",
        "Medical" => "tests/Modules/Medical.Tests/Infrastructure/Handlers/Prescriptions/CreateDraftTenantScopeTests.cs#ResolveTenantId_uses_the_authenticated_request_tenant",
        "Sea" => "tests/Modules/Sea.Tests/SeaAuthorizationHandlerTests.cs#SeaAuthorizationHandlerTests",
        "Communications" when operation.Classification == "anonymous" => "src/Hosts/Web/tests/two-host/authenticated-boundary.spec.ts#authorization-matrix:intentional-anonymous",
        "Social" when operation.Classification == "anonymous" => "src/Hosts/Web/tests/two-host/authenticated-boundary.spec.ts#authorization-matrix:intentional-anonymous",
        _ => "src/Hosts/Web/tests/two-host/authenticated-boundary.spec.ts#authorization-matrix:main-module-boundary"
    };

    private static string ExpectedAuthentication(string statement)
    {
        Match policy = PolicyRegex().Match(statement);
        if (policy.Success) return $"BrowserOrBearer + Policy:{policy.Groups[1].Value.Trim()}";
        Match requirement = RequireAuthorizationRegex().Match(statement);
        return requirement.Success
            ? $"BrowserOrBearer + Policy:{requirement.Groups[1].Value.Trim()}"
            : "BrowserOrBearer";
    }

    private static IReadOnlyList<MatrixRow> ParseRows(string text, List<ArchitectureViolation> violations)
    {
        int start = text.IndexOf(StartMarker, StringComparison.Ordinal);
        int end = text.IndexOf(EndMarker, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            violations.Add(new(MatrixPath, 1, "AuthorizationMatrixSectionMissing", MatrixPath,
                $"Add one checked operation table between {StartMarker} and {EndMarker}."));
            return [];
        }

        string section = text[(start + StartMarker.Length)..end];
        int startLine = text[..start].Count(character => character == '\n') + 1;
        List<MatrixRow> rows = [];
        string[] lines = section.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (!line.StartsWith('|') || line.Contains("Operation ID", StringComparison.Ordinal) || line.Contains("---", StringComparison.Ordinal)) continue;
            string[] cells = line.Trim('|').Split('|').Select(cell => cell.Trim().Trim('`')).ToArray();
            if (cells.Length != 9)
            {
                violations.Add(new(MatrixPath, startLine + index + 1, "AuthorizationMatrixRowMalformed", line,
                    "Use exactly nine matrix columns and do not place a pipe character inside a cell."));
                continue;
            }

            rows.Add(new(cells[0], cells[1], cells[2], cells[3], cells[4], cells[5], cells[6], cells[7], cells[8], startLine + index + 1));
        }

        return rows;
    }

    private static void ValidateRow(SourceIndex index, ModuleOperation operation, MatrixRow row, List<ArchitectureViolation> violations)
    {
        CheckEqual(operation, row, "ModuleOwnerMismatch", row.ModuleOwner, operation.Module, "module owner", violations);
        CheckEqual(operation, row, "RouteOwnerMismatch", row.RouteOwner, operation.RouteOwner, "route owner", violations);
        CheckEqual(operation, row, "AuthorizationClassificationMismatch", row.Classification, operation.Classification, "classification", violations);
        CheckEqual(operation, row, "AuthenticationDecisionMismatch", row.Authentication, operation.Authentication, "authentication scheme or policy", violations);
        CheckEqual(operation, row, "AnonymousRationaleMismatch", row.PublicRationale, operation.PublicRationale, "intentional-public rationale", violations);

        if (string.IsNullOrWhiteSpace(row.Authority) || row.Authority is "TBD" or "—")
            AddIncomplete(row, "tenant/resource/role authority", violations);
        if (!ReauthenticationValues.Contains(row.Reauthentication))
            AddIncomplete(row, "reauthentication requirement (Required, Not required, or Conditional)", violations);
        if (string.IsNullOrWhiteSpace(row.RegressionTest) || row.RegressionTest is "TBD" or "—")
        {
            AddIncomplete(row, "linked regression test", violations);
        }
        else
        {
            string[] reference = row.RegressionTest.Split('#', 2);
            string testPath = reference[0];
            if (!File.Exists(Path.Combine(index.Root, testPath.Replace('/', Path.DirectorySeparatorChar))))
            {
                violations.Add(new(MatrixPath, row.Line, "AuthorizationMatrixTestMissing", row.OperationId,
                    $"Linked regression test path does not exist: {testPath}."));
            }
            else if (reference.Length != 2 || string.IsNullOrWhiteSpace(reference[1]) ||
                     !File.ReadAllText(Path.Combine(index.Root, testPath.Replace('/', Path.DirectorySeparatorChar)))
                         .Contains(reference[1], StringComparison.Ordinal))
            {
                violations.Add(new(MatrixPath, row.Line, "AuthorizationMatrixTestReferenceMissing", row.OperationId,
                    $"Linked regression test reference does not exist in {testPath}."));
            }
        }
    }

    private static void CheckEqual(ModuleOperation operation, MatrixRow row, string kind, string actual, string expected, string field, List<ArchitectureViolation> violations)
    {
        if (string.Equals(actual, expected, StringComparison.Ordinal)) return;
        violations.Add(new(MatrixPath, row.Line, kind, row.OperationId,
            $"Set {field} to '{expected}' to match {operation.RouteOwner}:{operation.Line}."));
    }

    private static void AddIncomplete(MatrixRow row, string field, List<ArchitectureViolation> violations) =>
        violations.Add(new(MatrixPath, row.Line, "AuthorizationMatrixEvidenceMissing", row.OperationId,
            $"Provide the {field}; placeholders are not accepted."));

    [GeneratedRegex(@"Policy\s*=\s*([^,}\r\n]+)")]
    private static partial Regex PolicyRegex();

    [GeneratedRegex(@"RequireAuthorization\(\s*([^,)\r\n]+)")]
    private static partial Regex RequireAuthorizationRegex();

    internal sealed record ModuleOperation(
        string OperationId,
        string Module,
        string RouteOwner,
        int Line,
        string Classification,
        string Authentication,
        string PublicRationale);

    private sealed record MatrixRow(
        string OperationId,
        string ModuleOwner,
        string RouteOwner,
        string Classification,
        string Authentication,
        string Authority,
        string Reauthentication,
        string PublicRationale,
        string RegressionTest,
        int Line);
}
