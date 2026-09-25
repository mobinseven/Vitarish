using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vitarish.Tools.Architecture;

/// <summary>
/// This is deliberately a source/configuration check. It describes what the
/// repository declares; it does not claim that a deployed listener, TLS stack,
/// authorization policy, or provider is behaving correctly at runtime.
/// </summary>
internal static class SecurityBaselineCheck
{
    private static readonly HashSet<string> ModuleMappings =
        ["MapModuleGet", "MapModulePost", "MapModulePut", "MapModuleDelete", "MapModulePatch", "MapModuleHead"];

    private static readonly Dictionary<string, string> Methods = new(StringComparer.Ordinal)
    {
        ["MapModuleGet"] = "GET", ["MapModulePost"] = "POST", ["MapModulePut"] = "PUT",
        ["MapModuleDelete"] = "DELETE", ["MapModulePatch"] = "PATCH", ["MapModuleHead"] = "HEAD",
        ["MapGet"] = "GET", ["MapPost"] = "POST", ["MapPut"] = "PUT", ["MapDelete"] = "DELETE",
        ["MapPatch"] = "PATCH", ["MapMethods"] = "dynamic"
    };

    private static readonly HashSet<string> RawMappings =
        ["MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods"];

    private static readonly Dictionary<string, string> DefaultStatuses = new(StringComparer.Ordinal)
    {
        ["MapModuleGet"] = "200", ["MapModulePost"] = "200", ["MapModulePut"] = "200",
        ["MapModulePatch"] = "200", ["MapModuleHead"] = "200", ["MapModuleDelete"] = "204"
    };

    private static readonly Regex OperationIdPattern =
        new("^[A-Z][A-Za-z0-9]+(?:_[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Profiles =
        ["ProtectedJson", "AnonymousJson", "AnonymousWebhook", "MultipartUpload", "BinaryDownload", "Streaming"];

    private const string BaseEndpointPath = "src/Modules/Base/Core/Endpoints/";
    public static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        violations.AddRange(DiscoverEndpointViolations(index));
        violations.AddRange(CheckDeploymentBundles(index.Root));
        violations.AddRange(CheckSourceSecuritySurface(index));
        return violations;
    }

    internal static IReadOnlyList<ArchitectureViolation> RunEndpoints(SourceIndex index) =>
        DiscoverEndpointViolations(index);

    internal static IReadOnlyList<ArchitectureViolation> RunSourceSecuritySurface(SourceIndex index) =>
        CheckSourceSecuritySurface(index);

    private static IReadOnlyList<ArchitectureViolation> CheckDeploymentBundles(string root)
    {
        List<ArchitectureViolation> violations = [];
        string deploymentRoot = Path.Combine(root, "src", "Hosts", "deployment");
        string ingressPath = Path.Combine(deploymentRoot, "docker-compose.ingress.yml");
        string caddyPath = Path.Combine(deploymentRoot, "Caddyfile");
        string ingress = ReadRequired(ingressPath, violations);
        string caddy = ReadRequired(caddyPath, violations);

        foreach (string environment in new[] { "test", "production" })
        {
            string composePath = Path.Combine(deploymentRoot, $"docker-compose.{environment}.yml");
            string compose = ReadRequired(composePath, violations);
            string examplePath = Path.Combine(deploymentRoot, $".env.{environment}.example");
            string example = ReadRequired(examplePath, violations);
            if (Regex.IsMatch(compose, @"(?im)^\s*image:\s*[^\r\n]*:latest\s*$"))
                violations.Add(DeploymentViolation(composePath, "MutableImageReference", "Use a versioned or digest-pinned image reference."));
            if (Regex.IsMatch(example, @"(?im)^VITARISH_.*_IMAGE=.*:latest\s*$"))
                violations.Add(DeploymentViolation(examplePath, "MutableImageReference", "The committed environment contract must not suggest a mutable latest image."));
            if (Regex.IsMatch(compose, @"(?im)^\s*ports:\s*"))
                violations.Add(DeploymentViolation(composePath, "PublishedEnvironmentPort", "Environment services must be reachable through the shared ingress only."));
            foreach (string service in new[] { "main", "private-practice-owner", "patients", "platform-admin", "pm", "knowledge-qa", "landing", "agent-runtime", "postgres", "redis", "aistor", "elasticsearch" })
            {
                if (!Regex.IsMatch(compose, $@"(?m)^\s{{2}}{Regex.Escape(service)}:\s*$"))
                    violations.Add(DeploymentViolation(composePath, "RequiredServiceMissing", $"Declare the {service} service in the {environment} bundle."));
            }
            foreach (Match image in Regex.Matches(compose, "(?im)^\\s*image:\\s*\\\"?[^\\r\\n]+"))
            {
                if (!Regex.IsMatch(image.Value, @"\$\{[A-Z0-9_]+:\?[^}]+\}"))
                    violations.Add(DeploymentViolation(composePath, "ImageVariableRequired", "Every service image must be supplied through a required environment variable."));
            }
            foreach (string dependency in new[] { "postgres", "redis", "aistor", "elasticsearch" })
            {
                if (!Regex.IsMatch(compose, $@"(?ms)^\s{{6}}{Regex.Escape(dependency)}:\s*\{{\s*condition:\s*service_healthy\s*\}}"))
                    violations.Add(DeploymentViolation(composePath, "HealthyDependencyMissing", $"Main must wait for healthy {dependency}."));
                if (!Regex.IsMatch(compose, $@"(?ms)^\s{{2}}{Regex.Escape(dependency)}:\s*.*?^\s{{4}}healthcheck:\s*"))
                    violations.Add(DeploymentViolation(composePath, "HealthcheckMissing", $"The {dependency} service must define a readiness healthcheck."));
            }
            foreach (string applicationService in new[] { "main", "private-practice-owner", "patients", "platform-admin", "pm", "knowledge-qa", "landing", "agent-runtime" })
            {
                if (!Regex.IsMatch(compose, $@"(?ms)^\s{{2}}{Regex.Escape(applicationService)}:\s*.*?^\s{{4}}healthcheck:\s*"))
                    violations.Add(DeploymentViolation(composePath, "HealthcheckMissing", $"The {applicationService} service must define a readiness healthcheck."));
            }
            if (!compose.Contains($"name: vitarish-{environment}", StringComparison.Ordinal))
                violations.Add(DeploymentViolation(composePath, "ProjectNameMissing", "Use a distinct environment Compose project name."));
            if (!compose.Contains("internal: true", StringComparison.Ordinal) || !compose.Contains("external: true", StringComparison.Ordinal))
                violations.Add(DeploymentViolation(composePath, "NetworkIsolationMissing", "Declare an internal application network and an external shared proxy network."));
            foreach (string ownedResource in new[] { "application", "proxy", "postgres", "redis", "aistor", "elasticsearch" })
            {
                string expected = $"vitarish-{environment}-{ownedResource}";
                if (!compose.Contains(expected, StringComparison.Ordinal))
                    violations.Add(DeploymentViolation(composePath, "EnvironmentResourceOwnershipMissing", $"Declare an environment-owned network or volume named {expected}."));
            }
        }

        if (Regex.IsMatch(ingress, @"(?im)^\s*image:\s*[^\r\n]*:latest\s*$"))
            violations.Add(DeploymentViolation(ingressPath, "MutableImageReference", "Use a versioned or digest-pinned ingress image."));
        if (!ingress.Contains("80:80/tcp", StringComparison.Ordinal) || !ingress.Contains("443:443/tcp", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(ingressPath, "IngressPortAllowlist", "The shared ingress must publish TCP 80 and TCP 443."));
        if (Regex.IsMatch(ingress, @"(?im)443/udp|docker\.sock|agent-runtime"))
            violations.Add(DeploymentViolation(ingressPath, "IngressBoundaryViolation", "The ingress must not enable HTTP/3, mount the Docker socket, or expose the agent runtime."));
        if (Regex.IsMatch(caddy, @"(?im)reverse_proxy[^\r\n]*agent-runtime"))
            violations.Add(DeploymentViolation(caddyPath, "PublicAgentRuntime", "The server-only agent runtime must remain private."));
        foreach (string host in new[] { "TEST_PUBLIC_HOST", "TEST_PATIENTS_HOST", "TEST_PLATFORM_ADMIN_HOST", "TEST_PM_HOST", "TEST_KNOWLEDGE_QA_HOST", "TEST_LANDING_HOST", "PRODUCTION_PUBLIC_HOST", "PRODUCTION_PATIENTS_HOST", "PRODUCTION_PLATFORM_ADMIN_HOST", "PRODUCTION_PM_HOST", "PRODUCTION_KNOWLEDGE_QA_HOST", "PRODUCTION_LANDING_HOST" })
        {
            if (!caddy.Contains("{$" + host + "}", StringComparison.Ordinal))
                violations.Add(DeploymentViolation(caddyPath, "RouteHostMissing", $"Declare the {host} route host in the shared ingress."));
        }

        return violations;
    }

    public static void RenderInventory(SourceIndex index)
    {
        foreach (DiscoveredEndpoint endpoint in DiscoverEndpoints(index))
        {
            UserDataOperationInventory inventory = CreateUserDataInventory(endpoint);
            Console.WriteLine($"endpoint={endpoint.OperationId}; method={endpoint.Method}; route={endpoint.Route}; " +
                              $"owner={endpoint.File.RelativePath}:{endpoint.Line}; auth={endpoint.Authentication}; " +
                              $"profile={endpoint.Profile}; contentType={endpoint.ContentType}; openapi={endpoint.OpenApiStatus}; " +
                              $"anonymous={endpoint.Anonymous}; audit={endpoint.Audit}; transfer={endpoint.Transfer}; " +
                              $"user-data-owner={inventory.Owner}; subject-session={inventory.SubjectAndSession}; " +
                              $"object-operations={inventory.ProtectedObjectAndOperations}; authorization={inventory.Authorization}; " +
                              $"direction={inventory.Direction}; format-size={inventory.FormatAndMaximumSize}; " +
                              $"secure-channel={inventory.SecureChannel}; required-audit={inventory.RequiredAudit}; " +
                              $"rate-restriction={inventory.RateRestriction}");
        }

        foreach (SourceFile file in index.Files.Where(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (file.Text.Contains("Cookies.Append", StringComparison.Ordinal))
                Console.WriteLine($"surface=cookie; owner={file.RelativePath}; disposition=source-options-reviewed");
            if (file.Text.Contains("AddHttpClient", StringComparison.Ordinal) || file.Text.Contains("new HttpClient", StringComparison.Ordinal))
                Console.WriteLine($"surface=outbound-client; owner={file.RelativePath}; disposition=call-site-reviewed");
            if (file.Text.Contains("ServerCertificateCustomValidationCallback", StringComparison.Ordinal) ||
                file.Text.Contains("RemoteCertificateValidationCallback", StringComparison.Ordinal))
                Console.WriteLine($"surface=tls-certificate-validation; owner={file.RelativePath}; disposition=callback-detected");
            if (file.Text.Contains("SHA256", StringComparison.Ordinal) || file.Text.Contains("HMAC", StringComparison.Ordinal) ||
                file.Text.Contains("RSA", StringComparison.Ordinal))
                Console.WriteLine($"surface=crypto; owner={file.RelativePath}; disposition=call-site-reviewed");
            if (file.RelativePath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"surface=migration; owner={file.RelativePath}; disposition=generated-schema-owner");
            if (file.Text.Contains("IdentityRole", StringComparison.Ordinal) || file.Text.Contains("RoleManager", StringComparison.Ordinal))
                Console.WriteLine($"surface=role; owner={file.RelativePath}; disposition=role-owner-reviewed");
        }

        string webSpecRoot = Path.Combine(index.Root, "src", "Hosts", "Web", "src", "api", "specs");
        if (Directory.Exists(webSpecRoot))
            Console.WriteLine($"surface=generated-api-consumer; owner=src/Hosts/Web/src/api/specs; files={Directory.EnumerateFiles(webSpecRoot, "*.json").Count()}; disposition=generated-source-reviewed");
    }

    private static IReadOnlyList<ArchitectureViolation> DiscoverEndpointViolations(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        foreach (DiscoveredEndpoint endpoint in DiscoverEndpoints(index, violations))
        {
            if (endpoint.Route is null)
                violations.Add(endpoint.Violation("RouteUnresolved", "Resolve the route from a source-owned constant or literal."));

            if (endpoint.Authentication is null)
                violations.Add(endpoint.Violation("AuthenticationUnresolved", "Declare protected, anonymous, or internal group authorization at the endpoint owner."));

            if (endpoint.OperationId is null)
                violations.Add(endpoint.Violation("OperationIdMissing", "Declare a stable operation ID at the endpoint owner."));
            else if (!OperationIdPattern.IsMatch(endpoint.OperationId))
                violations.Add(endpoint.Violation("OperationIdInvalid", "Use the stable PascalCase operation ID format."));

            if (endpoint.Anonymous && string.IsNullOrWhiteSpace(endpoint.AnonymousJustification))
                violations.Add(endpoint.Violation("AnonymousJustificationMissing", "Provide a non-empty typed anonymous justification."));

            if (endpoint.Audit is null)
                violations.Add(endpoint.Violation("AuditDispositionMissing", "Use the existing security audit filter or typed audit metadata."));

            if (endpoint.Transfer is null)
                violations.Add(endpoint.Violation("TransferDispositionMissing", "Declare an endpoint profile that classifies JSON, webhook, multipart, binary, or streaming transfer."));

            if (endpoint.OpenApiStatus is null)
                violations.Add(endpoint.Violation("OpenApiStatusMissing", "Declare an OpenAPI success status or a typed OpenAPI exclusion."));

            if (endpoint.Profile is null)
                violations.Add(endpoint.Violation("EndpointProfileMissing", "Declare one of the Base endpoint profiles."));
            else if (!Profiles.Contains(endpoint.Profile))
                violations.Add(endpoint.Violation("EndpointProfileInvalid", "Use one of the Base endpoint profiles."));

            if (endpoint.Profile is "BinaryDownload" or "Streaming" && string.IsNullOrWhiteSpace(endpoint.ContentType))
                violations.Add(endpoint.Violation("ContentTypeMissing", "Binary and streaming endpoints must declare a content type."));

            if (endpoint.Profile is "AnonymousWebhook" && endpoint.Webhook is null)
                violations.Add(endpoint.Violation("WebhookMetadataMissing", "Attach typed WebhookEndpointMetadata to the webhook endpoint."));

            UserDataOperationInventory inventory = CreateUserDataInventory(endpoint);
            if (inventory.IsIncomplete)
                violations.Add(endpoint.Violation("UserDataOperationDispositionMissing", "Declare a stable operation ID, trusted authentication, transfer profile/content type, and audit disposition so the evaluated user-data inventory can classify the route."));
        }

        return violations;
    }

    private static IEnumerable<DiscoveredEndpoint> DiscoverEndpoints(
        SourceIndex index,
        List<ArchitectureViolation>? parseViolations = null)
    {
        Dictionary<string, IReadOnlyDictionary<string, GroupFact>> allGroups = index.Files
            .Where(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(file => file.RelativePath, file => (IReadOnlyDictionary<string, GroupFact>)DiscoverGroups(file), StringComparer.OrdinalIgnoreCase);

        foreach (SourceFile file in index.Files.Where(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (file.RelativePath.StartsWith(BaseEndpointPath, StringComparison.OrdinalIgnoreCase))
                continue;

            SyntaxNode root = file.Tree.GetRoot();
            Dictionary<string, GroupFact> groups = DiscoverGroups(file);

            foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string mapping = RoslynSyntax.Name(invocation);
                if (!ModuleMappings.Contains(mapping) && !RawMappings.Contains(mapping))
                    continue;

                if (mapping == "MapMethods" && IsBaseHelperCall(invocation))
                    continue;

                DiscoveredEndpoint endpoint = CreateEndpoint(index, file, invocation, mapping, groups, allGroups, parseViolations);
                yield return endpoint;
            }
        }
    }

    private static DiscoveredEndpoint CreateEndpoint(
        SourceIndex index,
        SourceFile file,
        InvocationExpressionSyntax invocation,
        string mapping,
        IReadOnlyDictionary<string, GroupFact> groups,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, GroupFact>> allGroups,
        List<ArchitectureViolation>? parseViolations)
    {
        string receiver = invocation.Expression is MemberAccessExpressionSyntax member
            ? member.Expression.ToString()
            : string.Empty;
        GroupFact? group = ResolveGroup(index, file, invocation, receiver, groups, allGroups);

        string? routePart = ResolveString(file, Argument(invocation, "pattern", 0)?.Expression);
        string? route = routePart is null || group?.Prefix is null
            ? routePart
            : CombineRoutes(group.Prefix, routePart);
        string method = mapping == "MapMethods"
            ? ResolveMethods(file, Argument(invocation, "httpMethods", 1)?.Expression)
            : Methods[mapping];

        string? operationId = null;
        string? profile = null;
        string? contentType = null;
        string? openApiStatus = null;
        string? anonymousJustification = group?.Justification;
        string? webhook = null;
        string? audit = null;
        bool hasTypedConvention = false;
        bool excludedFromOpenApi = false;
        ExpressionSyntax? profileExpression = null;
        ExpressionSyntax? contentTypeExpression = null;

        if (ModuleMappings.Contains(mapping))
        {
            operationId = ResolveString(file, Argument(invocation, "operationId", 2)?.Expression);
            profileExpression = Argument(invocation, "profile", ProfilePosition(mapping))?.Expression;
            profile = profileExpression is null
                ? mapping == "MapModuleHead" ? "BinaryDownload" : "ProtectedJson"
                : ResolveProfile(profileExpression);
            contentTypeExpression = Argument(invocation, "contentType", ContentTypePosition(mapping))?.Expression;
            contentType = ResolveString(file, contentTypeExpression);
            openApiStatus = ResolveStatus(file, Argument(invocation, "successStatusCode", StatusPosition(mapping))?.Expression)
                ?? DefaultStatuses.GetValueOrDefault(mapping);
            anonymousJustification = ResolveString(file, Argument(invocation, "anonymousJustification", AnonymousJustificationPosition(mapping))?.Expression)
                ?? anonymousJustification;
            hasTypedConvention = true;
        }
        else
        {
            ExpressionStatementSyntax? statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            string statementText = statement?.ToString() ?? invocation.Parent?.ToString() ?? string.Empty;
            excludedFromOpenApi = statementText.Contains("ExcludeFromOpenApiWithJustification", StringComparison.Ordinal) || group?.ExcludedFromOpenApi == true;
            ObjectCreationExpressionSyntax? convention = statement?.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .FirstOrDefault(objectCreation => objectCreation.Type.ToString().EndsWith("ModuleEndpointOpenApiConvention", StringComparison.Ordinal));
            if (convention is not null)
            {
                hasTypedConvention = true;
                operationId = ResolveString(file, convention.ArgumentList?.Arguments.FirstOrDefault()?.Expression);
                openApiStatus = ResolveStatus(file, convention.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression);
                profileExpression = convention.ArgumentList?.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "Profile")?.Expression;
                profile = ResolveProfile(profileExpression) ?? "ProtectedJson";
                contentTypeExpression = convention.ArgumentList?.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "ContentType")?.Expression;
                contentType = ResolveString(file, contentTypeExpression);
            }

            if (statementText.Contains("SecurityAuditEndpointFilter", StringComparison.Ordinal))
                audit = "SecurityAuditEndpointFilter";
            if (statementText.Contains("WebhookEndpointMetadata", StringComparison.Ordinal) ||
                file.Text.Contains("new WebhookEndpointMetadata", StringComparison.Ordinal))
                webhook = "typed-webhook-metadata";
        }

        if (profile is not null)
        {
            contentType ??= profile switch
            {
                "ProtectedJson" or "AnonymousJson" or "AnonymousWebhook" => "application/json",
                "MultipartUpload" => "multipart/form-data",
                "BinaryDownload" => "application/octet-stream",
                "Streaming" => "application/x-ndjson",
                _ => null
            };

            if (audit is null && (method is "GET" or "HEAD"))
                audit = "not-required-for-read";
            else if (audit is null && profile == "AnonymousWebhook" && webhook is not null)
                audit = "webhook-exempt-by-profile";
            else if (audit is null && ModuleMappings.Contains(mapping))
                audit = profile == "AnonymousWebhook"
                    ? "webhook-exempt-by-profile"
                    : profile == "AnonymousJson"
                        ? "SecurityAuditEndpointFilter-conditional"
                        : "SecurityAuditEndpointFilter";

            if (profile == "AnonymousWebhook" && file.Text.Contains("new WebhookEndpointMetadata", StringComparison.Ordinal))
                webhook = "typed-webhook-metadata";
        }

        if (excludedFromOpenApi && hasTypedConvention)
            openApiStatus ??= "excluded:" + ExtractExclusionJustification(file, invocation, group);

        if (mapping == "MapMethods" && method == "dynamic")
            parseViolations?.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "RouteMethodUnresolved", mapping,
                "Use a statically declared HTTP method set."));

        if (group?.Kind == GroupKind.Anonymous || group?.Kind == GroupKind.Internal)
        {
            if (profile is not null && profile != "AnonymousJson" && profile != "AnonymousWebhook")
                parseViolations?.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "AuthenticationProfileConflict", mapping,
                    "An anonymous/internal group endpoint must use an anonymous or webhook profile."));
        }

        string? authentication = ResolveEffectiveAuthorization(group?.Authentication, invocation);
        bool anonymous = group?.Kind is GroupKind.Anonymous or GroupKind.Internal ||
                         profile is "AnonymousJson" or "AnonymousWebhook";

        return new DiscoveredEndpoint(
            file,
            RoslynSyntax.Line(invocation),
            route,
            method,
            operationId,
            authentication,
            profile,
            contentType,
            openApiStatus,
            anonymous,
            anonymousJustification,
            audit,
            TransferFor(profile),
            webhook,
            mapping,
            group?.ExcludedFromOpenApi == true || excludedFromOpenApi);
    }

    private static GroupFact? ResolveGroup(
        SourceIndex index,
        SourceFile file,
        InvocationExpressionSyntax endpoint,
        string receiver,
        IReadOnlyDictionary<string, GroupFact> localGroups,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, GroupFact>> allGroups)
    {
        if (localGroups.TryGetValue(receiver, out GroupFact? local))
            return local;

        MethodDeclarationSyntax? owner = endpoint.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        ParameterSyntax? groupParameter = owner?.ParameterList.Parameters.FirstOrDefault(parameter => parameter.Identifier.ValueText == receiver);
        if (owner is null || groupParameter is null)
            return null;

        foreach (SourceFile callerFile in index.Files)
        {
            foreach (InvocationExpressionSyntax caller in callerFile.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                         .Where(candidate => RoslynSyntax.Name(candidate) == owner.Identifier.ValueText && candidate != endpoint))
            {
                string? supplied = caller.Expression is MemberAccessExpressionSyntax member
                    ? member.Expression.ToString()
                    : caller.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString();
                if (supplied is null)
                    continue;

                if (allGroups.TryGetValue(callerFile.RelativePath, out IReadOnlyDictionary<string, GroupFact>? callerGroups) &&
                    callerGroups.TryGetValue(supplied, out GroupFact? callerGroup))
                    return callerGroup;
            }
        }

        return null;
    }

    private static Dictionary<string, GroupFact> DiscoverGroups(SourceFile file)
    {
        Dictionary<string, GroupFact> groups = new(StringComparer.Ordinal);
        SyntaxNode root = file.Tree.GetRoot();
        foreach (VariableDeclaratorSyntax variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            InvocationExpressionSyntax? creation = variable.Initializer?.Value.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(invocation => RoslynSyntax.Name(invocation) is "MapProtectedModuleGroup" or "MapAnonymousModuleGroup" or
                    "MapInternalServiceModuleGroup" or "MapGroup");
            if (creation is null)
                continue;

            string name = variable.Identifier.ValueText;
            string method = RoslynSyntax.Name(creation);
            string? prefix = ResolveString(file, Argument(creation, "prefix", 0)?.Expression);
            GroupKind kind = method switch
            {
                "MapProtectedModuleGroup" => GroupKind.Protected,
                "MapAnonymousModuleGroup" => GroupKind.Anonymous,
                "MapInternalServiceModuleGroup" => GroupKind.Internal,
                _ => GroupKind.Unknown
            };
            ExpressionSyntax? schemeExpression = Argument(creation, "scheme", 1)?.Expression;
            string? scheme = schemeExpression is null ? "BrowserOrBearer" : ResolveString(file, schemeExpression);
            string? justification = ResolveString(file, Argument(creation, "justification", 1)?.Expression);
            bool excluded = file.Text.Contains($"{name}.ExcludeFromOpenApiWithJustification", StringComparison.Ordinal);
            ExpressionSyntax? tenantAccessExpression = Argument(creation, "requireTenantAccess", 2)?.Expression;
            bool? requireTenantAccess = tenantAccessExpression is null ? true : ResolveBoolean(file, tenantAccessExpression);
            string? authentication = kind switch
            {
                GroupKind.Protected when scheme is not null && requireTenantAccess is not null => "scheme=" + scheme + "; policy=" +
                    (requireTenantAccess == false ? "none" : "TenantAccess"),
                GroupKind.Anonymous => "anonymous",
                GroupKind.Internal => "internal-anonymous;signed-callback-required",
                _ => null
            };
            groups[name] = new(prefix, kind, authentication, justification, excluded);
        }

        return groups;
    }

    private static IReadOnlyList<ArchitectureViolation> CheckSourceSecuritySurface(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        foreach (SourceFile file in index.Files)
        {
            if (!file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            if (file.Text.Contains("AllowAnyOrigin", StringComparison.Ordinal) ||
                file.Text.Contains("AllowAnyMethod", StringComparison.Ordinal))
                violations.Add(new(file.RelativePath, 1, "UnsafeCorsPolicy", "CORS", "Use the evaluated configuration's explicit HTTPS origins and methods."));

            foreach (InvocationExpressionSyntax invocation in file.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string method = RoslynSyntax.Name(invocation);
                if (method is "MapMethods" && !file.RelativePath.StartsWith(BaseEndpointPath, StringComparison.OrdinalIgnoreCase))
                {
                    string methodSet = ResolveMethods(file, Argument(invocation, "httpMethods", 1)?.Expression);
                    if (methodSet == "dynamic")
                        violations.Add(new(file.RelativePath, RoslynSyntax.Line(invocation), "RouteMethodUnresolved", method,
                            "Declare the HTTP methods as a static source-owned set."));
                }
            }

            if (file.Text.Contains("ServerCertificateCustomValidationCallback", StringComparison.Ordinal) ||
                file.Text.Contains("RemoteCertificateValidationCallback", StringComparison.Ordinal))
                violations.Add(new(file.RelativePath, 1, "CertificateValidationOverride", "TLS", "Use native certificate and hostname validation; no callback bypass is permitted."));

            if (Regex.IsMatch(file.Text, @"(?i)UseSsl\s*=\s*false|UseSsl\s*:\s*false"))
                violations.Add(new(file.RelativePath, 1, "OutboundTlsDisabled", "UseSsl", "Enabled evaluated provider links must use the declared TLS/peer-validation path."));

            foreach (InvocationExpressionSyntax append in file.Tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                         .Where(invocation => RoslynSyntax.Name(invocation) == "Append" && invocation.Expression.ToString().Contains("Cookies", StringComparison.Ordinal)))
            {
                string nearby = file.Text[Math.Max(0, append.SpanStart)..Math.Min(file.Text.Length, append.Span.End + 800)];
                if (!nearby.Contains("HttpOnly", StringComparison.Ordinal) || !nearby.Contains("Secure", StringComparison.Ordinal) ||
                    !nearby.Contains("SameSite", StringComparison.Ordinal))
                    violations.Add(new(file.RelativePath, RoslynSyntax.Line(append), "CookieDispositionMissing", "Cookies.Append", "Declare HttpOnly, Secure, and SameSite at the cookie owner."));
            }
        }

        return violations;
    }

    private static string ReadRequired(string path, List<ArchitectureViolation> violations)
    {
        if (!File.Exists(path))
        {
            violations.Add(DeploymentViolation(path, "RequiredDeploymentArtifactMissing", "The deployment reference artifact is required for source reconciliation."));
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    private static ArchitectureViolation DeploymentViolation(string path, string kind, string remediation) =>
        new(Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace(Path.DirectorySeparatorChar, '/'), 1, kind, "DeploymentBundles", remediation);

    private static ArgumentSyntax? Argument(InvocationExpressionSyntax invocation, string name, int position) =>
        invocation.ArgumentList.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == name) ??
        (position < 0 ? null : invocation.ArgumentList.Arguments.Where(argument => argument.NameColon is null).ElementAtOrDefault(position));

    private static string? ResolveString(SourceFile file, ExpressionSyntax? expression)
    {
        if (expression is null)
            return null;
        if (expression.ToString() == "string.Empty")
            return string.Empty;
        Optional<object?> constant = file.Model.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is string value)
            return value;
        if (expression is InterpolatedStringExpressionSyntax interpolated && interpolated.Contents.All(content => content is InterpolatedStringTextSyntax))
            return string.Concat(interpolated.Contents.OfType<InterpolatedStringTextSyntax>().Select(content => content.TextToken.ValueText));
        if (expression is BinaryExpressionSyntax binary && binary.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression))
        {
            string? left = ResolveString(file, binary.Left);
            string? right = ResolveString(file, binary.Right);
            return left is null || right is null ? null : left + right;
        }
        return null;
    }

    private static string? ResolveStatus(SourceFile file, ExpressionSyntax? expression)
    {
        if (expression is null)
            return null;
        Optional<object?> value = file.Model.GetConstantValue(expression);
        if (value.HasValue && value.Value is int status)
            return status.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Match numeric = Regex.Match(expression.ToString(), @"Status(\d{3})", RegexOptions.CultureInvariant);
        return numeric.Success ? numeric.Groups[1].Value : int.TryParse(expression.ToString(), out int parsed) ? parsed.ToString() : null;
    }

    private static string? ResolveProfile(ExpressionSyntax? expression)
    {
        string value = expression?.ToString() ?? string.Empty;
        int separator = value.LastIndexOf('.');
        return value.Contains("ModuleEndpointProfile", StringComparison.Ordinal) && separator >= 0
            ? value[(separator + 1)..]
            : string.IsNullOrWhiteSpace(value) ? "ProtectedJson" : null;
    }

    private static bool? ResolveBoolean(SourceFile file, ExpressionSyntax? expression)
    {
        Optional<object?> value = expression is null ? default : file.Model.GetConstantValue(expression);
        return value.HasValue && value.Value is bool boolean ? boolean : bool.TryParse(expression?.ToString(), out bool parsed) ? parsed : null;
    }

    private static string ResolveMethods(SourceFile file, ExpressionSyntax? expression)
    {
        if (expression is ArrayCreationExpressionSyntax array)
        {
            string[] methods = array.Initializer?.Expressions.Select(item => ResolveString(file, item)).Where(item => item is not null).Cast<string>().ToArray() ?? [];
            return methods.Length == 0 ? "dynamic" : string.Join(',', methods);
        }
        return ResolveString(file, expression) ?? "dynamic";
    }

    private static string? ResolveEffectiveAuthorization(string? groupAuthentication, InvocationExpressionSyntax invocation)
    {
        ExpressionStatementSyntax? statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        string text = statement?.ToString() ?? string.Empty;
        if (text.Contains("AllowAnonymous", StringComparison.Ordinal))
            return "anonymous";

        Match authorization = Regex.Match(text, @"\.RequireAuthorization\s*(?:\((?<argument>[^)]*)\))?", RegexOptions.CultureInvariant);
        if (!authorization.Success)
            return groupAuthentication;

        string argument = authorization.Groups["argument"].Value.Trim();
        string policy = string.IsNullOrWhiteSpace(argument) ? "default" : argument;
        return string.IsNullOrWhiteSpace(groupAuthentication)
            ? "policy=" + policy
            : groupAuthentication + "; endpoint-policy=" + policy;
    }

    private static string CombineRoutes(string prefix, string pattern)
    {
        if (string.IsNullOrEmpty(prefix)) return pattern;
        if (string.IsNullOrEmpty(pattern)) return prefix;
        return "/" + string.Join('/', (prefix + "/" + pattern).Split('/', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? TransferFor(string? profile) => profile switch
    {
        "ProtectedJson" or "AnonymousJson" => "json-request-response",
        "AnonymousWebhook" => "inbound-webhook",
        "MultipartUpload" => "inbound-multipart-file",
        "BinaryDownload" => "outbound-binary",
        "Streaming" => "streaming",
        "LongRunningCommand" => "json-long-running-command",
        _ => null
    };

    private static UserDataOperationInventory CreateUserDataInventory(DiscoveredEndpoint endpoint)
    {
        string? operation = endpoint.Method switch
        {
            "GET" or "HEAD" => "read-or-metadata",
            "POST" => "create-or-command",
            "PUT" or "PATCH" => "change",
            "DELETE" => "delete",
            _ => null
        };
        string? formatAndMaximumSize = endpoint.Profile switch
        {
            "MultipartUpload" => "declared-multipart-format; FileTransferPolicy.MaximumContentBytes",
            "BinaryDownload" => "verified-stored-binary; FileTransferPolicy.MaximumContentBytes",
            "Streaming" => endpoint.ContentType is null ? null : endpoint.ContentType + "; streaming-boundary",
            _ => endpoint.ContentType is null ? null : endpoint.ContentType + "; route-contract-boundary"
        };

        return new UserDataOperationInventory(
            Owner: endpoint.File.RelativePath,
            SubjectAndSession: endpoint.Authentication,
            ProtectedObjectAndOperations: endpoint.OperationId is null || operation is null ? null : endpoint.OperationId + ":" + operation,
            Authorization: endpoint.Authentication,
            Direction: endpoint.Transfer,
            FormatAndMaximumSize: formatAndMaximumSize,
            SecureChannel: endpoint.Authentication is null ? null : "evaluated-host-confidentiality-and-integrity-channel",
            RequiredAudit: endpoint.Audit,
            RateRestriction: "not-selected");
    }

    private static string ExtractExclusionJustification(SourceFile file, InvocationExpressionSyntax invocation, GroupFact? group) =>
        group?.ExcludedFromOpenApi == true ? "group-typed-exclusion" : "typed-exclusion";

    private static int ProfilePosition(string mapping) => mapping == "MapModuleHead" ? 5 : 5;
    private static int ContentTypePosition(string mapping) => mapping == "MapModuleHead" ? 6 : 7;
    private static int StatusPosition(string mapping) => 4;
    private static int AnonymousJustificationPosition(string mapping) => mapping == "MapModuleHead" ? -1 : 6;

    private static bool IsBaseHelperCall(InvocationExpressionSyntax invocation) =>
        invocation.SyntaxTree.FilePath.Replace('\\', '/').Contains(BaseEndpointPath, StringComparison.OrdinalIgnoreCase);

    private enum GroupKind { Unknown, Protected, Anonymous, Internal }

    private sealed record GroupFact(
        string? Prefix,
        GroupKind Kind,
        string? Authentication,
        string? Justification,
        bool ExcludedFromOpenApi);

    private sealed record DiscoveredEndpoint(
        SourceFile File,
        int Line,
        string? Route,
        string Method,
        string? OperationId,
        string? Authentication,
        string? Profile,
        string? ContentType,
        string? OpenApiStatus,
        bool Anonymous,
        string? AnonymousJustification,
        string? Audit,
        string? Transfer,
        string? Webhook,
        string Mapping,
        bool ExcludedFromOpenApi)
    {
        public ArchitectureViolation Violation(string kind, string remediation) =>
            new(File.RelativePath, Line, kind, Mapping, remediation);
    }

    private sealed record UserDataOperationInventory(
        string? Owner,
        string? SubjectAndSession,
        string? ProtectedObjectAndOperations,
        string? Authorization,
        string? Direction,
        string? FormatAndMaximumSize,
        string? SecureChannel,
        string? RequiredAudit,
        string RateRestriction)
    {
        public bool IsIncomplete =>
            string.IsNullOrWhiteSpace(Owner) ||
            string.IsNullOrWhiteSpace(SubjectAndSession) ||
            string.IsNullOrWhiteSpace(ProtectedObjectAndOperations) ||
            string.IsNullOrWhiteSpace(Authorization) ||
            string.IsNullOrWhiteSpace(Direction) ||
            string.IsNullOrWhiteSpace(FormatAndMaximumSize) ||
            string.IsNullOrWhiteSpace(SecureChannel) ||
            string.IsNullOrWhiteSpace(RequiredAudit);
    }
}
