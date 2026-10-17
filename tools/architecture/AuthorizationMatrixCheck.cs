using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

internal static partial class AuthorizationMatrixCheck
{
    internal const string ExecutableDispositionMismatchKind = "UserAuthExecutableDispositionMismatch";
    internal const string ExecutableAuthorityMismatchKind = "UserAuthExecutableAuthorityMismatch";
    internal const string UserAuthRoleObjectTestReferenceMissingKind = "UserAuthRoleObjectTestReferenceMissing";
    internal const string UserAuthRoleObjectDispositionInvalidKind = "UserAuthRoleObjectDispositionInvalid";
    private const string MatrixPath = "docs/security/authorization-matrix.md";
    private const string StartMarker = "<!-- main-module-authorization-matrix:start -->";
    private const string EndMarker = "<!-- main-module-authorization-matrix:end -->";
    private const string UserAuthStartMarker = "<!-- user-auth-role-object-matrix:start -->";
    private const string UserAuthEndMarker = "<!-- user-auth-role-object-matrix:end -->";
    private const string AllowDisposition = "Allow";
    private const string DenyDisposition = "Deny";
    private const string SelfOnlyDisposition = "Self only";
    private const string CurrentTenantOnlyDisposition = "Current tenant only";
    private const string CurrentSessionOnlyDisposition = "Current session only";
    private const string PreAuthOnlyDisposition = "Pre-auth only";
    private static readonly HashSet<string> UserDataCategories =
        ["Account record", "Account profile metadata", "Authentication credential", "Authentication challenge", "Authentication session", "Authentication history", "Security policy", "Tenant membership", "Role assignment", "User security metadata"];
    private static readonly HashSet<string> UserOperations =
        ["Read", "Create", "Delete", "Change", "Acknowledge", "Authenticate", "End session", "Validate", "Change permissions"];
    private static readonly HashSet<string> SubjectDispositions =
        ["Allow", "Deny", "Self only", "Current tenant only", "Current session only", "Pre-auth only"];
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
        ValidateUserAuthRoleObjectMatrix(index, matrixText, operations, violations);

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

    private static void ValidateUserAuthRoleObjectMatrix(
        SourceIndex index,
        string text,
        IReadOnlyList<ModuleOperation> operations,
        List<ArchitectureViolation> violations)
    {
        int start = text.IndexOf(UserAuthStartMarker, StringComparison.Ordinal);
        int end = text.IndexOf(UserAuthEndMarker, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            violations.Add(new(MatrixPath, 1, "UserAuthRoleObjectMatrixMissing", MatrixPath,
                $"Add one checked Users and Authentication role/object table between {UserAuthStartMarker} and {UserAuthEndMarker}."));
            return;
        }

        string section = text[(start + UserAuthStartMarker.Length)..end];
        int startLine = text[..start].Count(character => character == '\n') + 1;
        List<UserAuthRow> rows = [];
        string[] lines = section.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (!line.StartsWith('|') || line.Contains("Operation ID", StringComparison.Ordinal) || line.Contains("---", StringComparison.Ordinal)) continue;
            string[] cells = line.Trim('|').Split('|').Select(cell => cell.Trim().Trim('`')).ToArray();
            if (cells.Length != 8)
            {
                violations.Add(new(MatrixPath, startLine + lineIndex + 1, "UserAuthRoleObjectMatrixRowMalformed", line,
                    "Use exactly eight columns: operation ID, data category, operation, three subject dispositions, owner, and regression test."));
                continue;
            }

            rows.Add(new(cells[0], cells[1], cells[2], cells[3], cells[4], cells[5], cells[6], cells[7], startLine + lineIndex + 1));
        }

        foreach (IGrouping<string, UserAuthRow> duplicate in rows.GroupBy(row => row.OperationId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            violations.Add(new(MatrixPath, duplicate.First().Line, "DuplicateUserAuthRoleObjectOperation", duplicate.Key,
                "Keep exactly one role/object disposition for each Users or Authentication HTTP operation."));
        }

        Dictionary<string, UserAuthRow> byOperation = rows
            .GroupBy(row => row.OperationId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        HashSet<string> expected = operations
            .Where(operation => operation.Module is "Users" or "Authentication")
            .Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string operationId in expected.Where(operationId => !byOperation.ContainsKey(operationId)))
            violations.Add(new(MatrixPath, startLine, "MissingUserAuthRoleObjectOperation", operationId,
                "Add a semantic data-category, operation, subject, owner, and regression-test disposition."));
        foreach (UserAuthRow row in rows.Where(row => !expected.Contains(row.OperationId)))
            violations.Add(new(MatrixPath, row.Line, "StaleUserAuthRoleObjectOperation", row.OperationId,
                "Remove this row or restore its Users or Authentication HTTP operation mapping."));

        foreach (UserAuthRow row in rows)
        {
            if (!UserDataCategories.Contains(row.DataCategory))
                AddUserAuthIncomplete(row, "a recognized passive-entity category", violations);
            if (!UserOperations.Contains(row.Operation))
                AddUserAuthIncomplete(row, "a recognized operation", violations);
            if (!SubjectDispositions.Contains(row.PlatformAdmin) || !SubjectDispositions.Contains(row.TenantOwner) || !SubjectDispositions.Contains(row.TenantUser))
                AddUserAuthIncomplete(row, "valid PlatformAdmin, TenantOwner, and TenantUser dispositions", violations);
            if (string.IsNullOrWhiteSpace(row.Authority) || row.Authority is "TBD" or "—")
                AddUserAuthIncomplete(row, "the owning authorization policy or subject/resource rule", violations);
            ValidateExecutableDisposition(index, operations, row, violations);
            if (string.IsNullOrWhiteSpace(row.RegressionTest) || row.RegressionTest is "TBD" or "—")
            {
                AddUserAuthIncomplete(row, "a linked regression test", violations);
                continue;
            }

            string[] reference = row.RegressionTest.Split('#', 2);
            string testPath = reference[0];
            string absoluteTestPath = Path.Combine(index.Root, testPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absoluteTestPath))
                violations.Add(new(MatrixPath, row.Line, "UserAuthRoleObjectTestMissing", row.OperationId, $"Linked regression test path does not exist: {testPath}."));
            else if (reference.Length != 2 || !IsTestCaseReference(absoluteTestPath, reference[1]))
                violations.Add(new(MatrixPath, row.Line, UserAuthRoleObjectTestReferenceMissingKind, row.OperationId, $"Linked regression test reference does not exist in {testPath}."));
        }
    }

    private static void ValidateExecutableDisposition(
        SourceIndex index,
        IReadOnlyList<ModuleOperation> operations,
        UserAuthRow row,
        List<ArchitectureViolation> violations)
    {
        ModuleOperation? operation = operations.FirstOrDefault(candidate => candidate.OperationId == row.OperationId);
        SourceFile? endpointFile = operation is null ? null : index.Files.FirstOrDefault(file => file.RelativePath == operation.RouteOwner);
        if (operation is null || endpointFile is null) return;

        InvocationExpressionSyntax? mapping = endpointFile.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation => ModuleMappings.Contains(RoslynSyntax.Name(invocation)) &&
                invocation.ArgumentList.Arguments.Any(argument => RoslynSyntax.ResolveString(endpointFile, argument.Expression) == row.OperationId));
        if (mapping is null)
        {
            AddUserAuthIncomplete(row, "an executable endpoint mapping that can be classified", violations);
            return;
        }

        ExpressionStatementSyntax? statement = mapping.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        string? handlerName = mapping.ArgumentList.Arguments.Where(argument => argument.NameColon is null)
            .ElementAtOrDefault(1)?.Expression is IdentifierNameSyntax handlerIdentifier
                ? handlerIdentifier.Identifier.ValueText
                : null;
        InvocationExpressionSyntax? authorization = statement?.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation => RoslynSyntax.Name(invocation) == "RequireAuthorization");
        if (operation.Classification == "anonymous")
        {
            if (!row.Authority.Contains("Anonymous endpoint mapping", StringComparison.Ordinal))
                AddExecutableAuthorityMismatch(row, "the anonymous endpoint mapping", violations);
            RequireDispositions(row, [PreAuthOnlyDisposition, PreAuthOnlyDisposition, PreAuthOnlyDisposition], violations);
            return;
        }

        if (authorization is not null)
        {
            string policyExpression = authorization.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() ?? string.Empty;
            if (policyExpression.Length > 0 && !row.Authority.Contains(policyExpression, StringComparison.Ordinal))
                AddExecutableAuthorityMismatch(row, policyExpression, violations);
            string? policyName = authorization.ArgumentList.Arguments.FirstOrDefault() is { } policyArgument
                ? RoslynSyntax.ResolveString(endpointFile, policyArgument.Expression)
                : null;
            (string[]? roles, bool classifiable, bool tenantOwnerAssertion) = ResolvePolicyRoles(index, policyName);
            if (classifiable && roles is not null)
            {
                string platformRole = ResolveNamedConstant(index, "IdentityDefinitions.PlatformAdmin") ?? string.Empty;
                string ownerRole = ResolveNamedConstant(index, "IdentityDefinitions.TenantOwner") ?? string.Empty;
                MethodDeclarationSyntax[] endpointMethods = endpointFile.Tree.GetRoot().DescendantNodes()
                    .OfType<MethodDeclarationSyntax>().ToArray();
                bool endpointChecksTenant = handlerName is not null && endpointMethods
                    .Where(method => method.Identifier.ValueText == handlerName)
                    .SelectMany(method => method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    .Any(call => endpointMethods.Any(helper => helper.Identifier.ValueText == RoslynSyntax.Name(call) &&
                        helper.ToString().Contains("requestContext.TenantId", StringComparison.Ordinal) &&
                        helper.ToString().Contains("CanManageTenantMembers", StringComparison.Ordinal)));
                RequireDispositions(row,
                    [roles.Contains(platformRole, StringComparer.Ordinal) ? AllowDisposition : DenyDisposition,
                     roles.Contains(ownerRole, StringComparer.Ordinal) ? tenantOwnerAssertion && endpointChecksTenant ? CurrentTenantOnlyDisposition : AllowDisposition : DenyDisposition,
                     DenyDisposition], violations);
                return;
            }

            if (!classifiable)
            {
                AddUserAuthIncomplete(row, "a policy whose executable role requirements can be resolved", violations);
                return;
            }
        }

        bool checksSelf = statement?.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .Any(invocation => RoslynSyntax.Name(invocation) == "AllowsSelf") == true ||
            handlerName is not null && endpointFile.Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText == handlerName)
                .SelectMany(method => method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Any(invocation => RoslynSyntax.Name(invocation) == "AllowsSelf");
        if (checksSelf)
        {
            if (!row.Authority.Contains("UserSubjectAccessPolicy", StringComparison.Ordinal))
                AddExecutableAuthorityMismatch(row, "UserSubjectAccessPolicy", violations);
            RequireDispositions(row, [SelfOnlyDisposition, SelfOnlyDisposition, SelfOnlyDisposition], violations);
            return;
        }

        if (!row.Authority.Contains("Authenticated", StringComparison.Ordinal))
            AddExecutableAuthorityMismatch(row, "the protected endpoint's authenticated subject/session owner", violations);
        string[] actual = [row.PlatformAdmin, row.TenantOwner, row.TenantUser];
        if (actual.Distinct(StringComparer.Ordinal).Count() != 1 || actual[0] is not (SelfOnlyDisposition or CurrentSessionOnlyDisposition))
            AddUserAuthIncomplete(row, "one authenticated subject/session scope shared by the protected endpoint's roles", violations);
    }

    private static (string[]? Roles, bool Classifiable, bool TenantOwnerAssertion) ResolvePolicyRoles(SourceIndex index, string? policyName)
    {
        if (policyName is null) return (null, false, false);
        foreach (SourceFile file in index.Files)
        foreach (InvocationExpressionSyntax addPolicy in file.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                     .Where(invocation => RoslynSyntax.Name(invocation) == "AddPolicy"))
        {
            if (addPolicy.ArgumentList.Arguments.Count < 2 ||
                RoslynSyntax.ResolveString(file, addPolicy.ArgumentList.Arguments[0].Expression) != policyName) continue;
            ExpressionSyntax definition = addPolicy.ArgumentList.Arguments[1].Expression;
            InvocationExpressionSyntax[] requirements = definition.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .Where(invocation => RoslynSyntax.Name(invocation) == "RequireRole").ToArray();
            if (requirements.Length == 0)
            {
                bool tenantOwnerAssertion = definition.ToString().Contains("CanManageAnyTenantMembers", StringComparison.Ordinal);
                if (!tenantOwnerAssertion) return (null, true, false);
                MethodDeclarationSyntax? assertionOwner = index.Files.SelectMany(source => source.Tree.GetRoot()
                        .DescendantNodes().OfType<MethodDeclarationSyntax>())
                    .FirstOrDefault(method => method.Identifier.ValueText == "CanManageAnyTenantMembers");
                string assertionBody = assertionOwner?.Body?.ToString() ?? assertionOwner?.ExpressionBody?.ToString() ?? string.Empty;
                if (assertionOwner is null ||
                    !assertionBody.Contains("IsPlatformAdministrator", StringComparison.Ordinal) ||
                    !assertionBody.Contains("IdentityDefinitions.TenantOwner", StringComparison.Ordinal))
                    return (null, false, false);
                string? platformAdmin = ResolveNamedConstant(index, "IdentityDefinitions.PlatformAdmin");
                string? tenantOwner = ResolveNamedConstant(index, "IdentityDefinitions.TenantOwner");
                return platformAdmin is null || tenantOwner is null
                    ? (null, false, false)
                    : ([platformAdmin, tenantOwner], true, true);
            }
            if (requirements.Length != 1) return (null, false, false);
            string[] roles = requirements.SelectMany(requirement => requirement.ArgumentList.Arguments
                    .Select(argument => RoslynSyntax.ResolveString(file, argument.Expression)))
                .Where(role => role is not null).Select(role => role!).ToArray();
            return roles.Length == 0 ? (null, false, false) : (roles, true, false);
        }

        return (null, false, false);
    }

    private static string? ResolveNamedConstant(SourceIndex index, string qualifiedName)
    {
        string typeName = qualifiedName.Split('.')[0];
        string memberName = qualifiedName.Split('.')[1];
        foreach (SourceFile file in index.Files)
        foreach (VariableDeclaratorSyntax variable in file.Tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (variable.Initializer is null || variable.Identifier.ValueText != memberName ||
                variable.FirstAncestorOrSelf<ClassDeclarationSyntax>()?.Identifier.ValueText != typeName) continue;
            string? value = RoslynSyntax.ResolveString(file, variable.Initializer.Value);
            if (value is not null) return value;
        }

        return null;
    }

    private static void RequireDispositions(UserAuthRow row, string[] expected, List<ArchitectureViolation> violations)
    {
        string[] actual = [row.PlatformAdmin, row.TenantOwner, row.TenantUser];
        if (actual.SequenceEqual(expected, StringComparer.Ordinal)) return;
        violations.Add(new(MatrixPath, row.Line, ExecutableDispositionMismatchKind, row.OperationId,
            $"Set PlatformAdmin, TenantOwner, and TenantUser to '{string.Join("', '", expected)}' to match the executable authorization owner."));
    }

    private static void AddExecutableAuthorityMismatch(UserAuthRow row, string owner, List<ArchitectureViolation> violations) =>
        violations.Add(new(MatrixPath, row.Line, ExecutableAuthorityMismatchKind, row.OperationId,
            $"Name the executable authorization owner '{owner}' in this row."));

    private static bool IsTestCaseReference(string path, string anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor)) return false;
        string text = File.ReadAllText(path);
        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
            string methodName = anchor.Split('.').Last();
            return tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Any(method => method.Identifier.ValueText == methodName &&
                    (method.AttributeLists.SelectMany(list => list.Attributes).Any(attribute =>
                         attribute.Name.ToString() is "Fact" or "Theory" or "FactAttribute" or "TheoryAttribute") ||
                     methodName.EndsWith("SelfTest", StringComparison.Ordinal)));
        }

        return Regex.IsMatch(text, "\\btest\\s*\\(\\s*\\\"" + Regex.Escape(anchor) + "\\\"\\s*,", RegexOptions.CultureInvariant);
    }

    private static void AddUserAuthIncomplete(UserAuthRow row, string field, List<ArchitectureViolation> violations) =>
        violations.Add(new(MatrixPath, row.Line, UserAuthRoleObjectDispositionInvalidKind, row.OperationId,
            $"Provide {field}; placeholders and unrecognized values are not accepted."));

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
        "Communications" when operation.Classification == "anonymous" => "src/Hosts/Web/tests/e2e/authenticated-boundary.spec.ts#authorization-matrix:intentional-anonymous",
        "Social" when operation.Classification == "anonymous" => "src/Hosts/Web/tests/e2e/authenticated-boundary.spec.ts#authorization-matrix:intentional-anonymous",
        _ => "src/Hosts/Web/tests/e2e/authenticated-boundary.spec.ts#authorization-matrix:main-module-boundary"
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

    private sealed record UserAuthRow(
        string OperationId,
        string DataCategory,
        string Operation,
        string PlatformAdmin,
        string TenantOwner,
        string TenantUser,
        string Authority,
        string RegressionTest,
        int Line);
}
