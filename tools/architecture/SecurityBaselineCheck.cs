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
    private const string ProfileIdentity = "vitarish-product-evaluated-v1.0";

    public static IReadOnlyList<ArchitectureViolation> Run(SourceIndex index)
    {
        List<ArchitectureViolation> violations = [];
        violations.AddRange(DiscoverEndpointViolations(index));
        violations.AddRange(CheckEvaluatedProfile(index.Root));
        violations.AddRange(CheckSourceSecuritySurface(index));
        return violations;
    }

    internal static IReadOnlyList<ArchitectureViolation> RunEndpoints(SourceIndex index) =>
        DiscoverEndpointViolations(index);

    internal static IReadOnlyList<ArchitectureViolation> RunSourceSecuritySurface(SourceIndex index) =>
        CheckSourceSecuritySurface(index);

    public static void RenderInventory(SourceIndex index)
    {
        foreach (DiscoveredEndpoint endpoint in DiscoverEndpoints(index))
        {
            Console.WriteLine($"endpoint={endpoint.OperationId}; method={endpoint.Method}; route={endpoint.Route}; " +
                              $"owner={endpoint.File.RelativePath}:{endpoint.Line}; auth={endpoint.Authentication}; " +
                              $"profile={endpoint.Profile}; contentType={endpoint.ContentType}; openapi={endpoint.OpenApiStatus}; " +
                              $"anonymous={endpoint.Anonymous}; audit={endpoint.Audit}; transfer={endpoint.Transfer}");
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

    private static IReadOnlyList<ArchitectureViolation> CheckEvaluatedProfile(string root)
    {
        List<ArchitectureViolation> violations = [];
        string profileRoot = Path.Combine(root, "docs", "security", "network-questionnaire-v1.0");
        string composePath = Path.Combine(root, "src", "Hosts", "docker-compose.product-evaluated-v1.0.yml");
        string caddyPath = Path.Combine(root, "src", "Hosts", "caddy", "Caddyfile.product-evaluated-v1.0");
        string configPath = Path.Combine(profileRoot, "evaluated-configuration.md");
        string topologyPath = Path.Combine(profileRoot, "topology.md");
        string toePath = Path.Combine(profileRoot, "toe-inventory.md");
        string transportInventoryPath = Path.Combine(profileRoot, "evaluated-transport-and-cryptography.md");
        string updateRunbookPath = Path.Combine(profileRoot, "manual-update-runbook.md");
        string compose = ReadRequired(composePath, violations);
        string caddy = ReadRequired(caddyPath, violations);
        string config = ReadRequired(configPath, violations);
        string topology = ReadRequired(topologyPath, violations);
        string toe = ReadRequired(toePath, violations);
        string transportInventory = ReadRequired(transportInventoryPath, violations);
        string updateRunbook = ReadRequired(updateRunbookPath, violations);

        RequireText(config, ProfileIdentity, configPath, violations);
        RequireText(topology, ProfileIdentity, topologyPath, violations);
        RequireText(toe, ProfileIdentity, toePath, violations);
        RequireText(transportInventory, ProfileIdentity, transportInventoryPath, violations);
        RequireText(updateRunbook, ProfileIdentity, updateRunbookPath, violations);
        RequireText(compose, "name: vitarish-product-evaluated-v1-0", composePath, violations);
        RequireText(compose, "internal: true", composePath, violations);
        RequireText(caddy, "protocols tls1.2 tls1.2", caddyPath, violations);
        RequireText(caddy, "ciphers TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256", caddyPath, violations);
        RequireText(caddy, "curves secp256r1", caddyPath, violations);
        RequireText(caddy, "alpn h1 h2", caddyPath, violations);
        RequireText(caddy, "reverse_proxy @api main:8080", caddyPath, violations);
        RequireText(topology, "same-machine", topologyPath, violations);
        RequireText(topology, "separate-machine", topologyPath, violations);
        RequireText(transportInventory, "basicConstraints CA=TRUE", transportInventoryPath, violations);
        RequireText(transportInventory, "CR-4/TSF-6 not triggered", transportInventoryPath, violations);
        RequireText(updateRunbook, "authorized administrator or developer", updateRunbookPath, violations);
        RequireText(compose, "egress:", composePath, violations);

        if (Regex.IsMatch(compose, @"(?im)^\s*image:\s*[^\r\n]*:latest\s*$"))
            violations.Add(ProfileViolation(composePath, "MutableImageReference", "Use a release-supplied versioned image reference."));
        if (Regex.IsMatch(compose, @"(?im)^\s*-\s*[^\r\n]*(?:5432|6379)(?::|/)") ||
            Regex.IsMatch(compose, @"(?im)^\s*-\s*[^\r\n]*:443/udp\s*$"))
            violations.Add(ProfileViolation(composePath, "ForbiddenPublishedPort", "Only Caddy TCP 80 and TCP 443 may be published."));
        if (Regex.IsMatch(caddy, "(?im)\\b(?:h3|http/3|quic)\\b|443/udp"))
            violations.Add(ProfileViolation(caddyPath, "ForbiddenHttp3Exposure", "The frozen TLS 1.2 profile must not enable HTTP/3 or QUIC."));
        if (Regex.IsMatch(caddy, "(?im)reverse_proxy[^\r\n]*agent-runtime"))
            violations.Add(ProfileViolation(caddyPath, "PublicAgentRuntime", "The server-only agent runtime must remain private."));

        string[] expectedHosts = [
            "VITARISH_PUBLIC_HOST", "VITARISH_PATIENTS_HOST", "VITARISH_PLATFORM_ADMIN_HOST",
            "VITARISH_PM_HOST", "VITARISH_KNOWLEDGE_QA_HOST", "VITARISH_LANDING_HOST"];
        foreach (string host in expectedHosts)
        {
            if (!compose.Contains(host, StringComparison.Ordinal) || !caddy.Contains(host, StringComparison.Ordinal))
                violations.Add(ProfileViolation(composePath, "HostBoundaryMissing", $"Declare {host} in both the evaluated Compose and Caddy owners."));
        }

        Match servicesMatch = Regex.Match(compose, @"(?ms)^services:\s*\r?\n(?<services>.*?)(?=^networks:\s*$)");
        string serviceSection = servicesMatch.Success ? servicesMatch.Groups["services"].Value : string.Empty;
        foreach (Match service in Regex.Matches(serviceSection, @"(?ms)^  (?<name>[a-z0-9-]+):\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:|\z)"))
        {
            string name = service.Groups["name"].Value;
            string body = service.Groups["body"].Value;
            if (body.Contains("ports:", StringComparison.Ordinal) && name != "caddy")
                violations.Add(ProfileViolation(composePath, "NonIngressPublishedPort", $"Service {name} publishes a host port."));
            if (!body.Contains("image:", StringComparison.Ordinal) && name is not "networks" and not "volumes" and not "secrets")
                violations.Add(ProfileViolation(composePath, "ServiceImageMissing", $"Service {name} has no image declaration."));
        }

        Match mainNetworks = Regex.Match(serviceSection, @"(?ms)^  main:\s*(?<body>.*?)(?=^  [a-z0-9-]+:|\z)");
        if (!mainNetworks.Success || !Regex.IsMatch(mainNetworks.Groups["body"].Value, @"(?m)^    networks:\s*\[application, egress\]\s*$"))
            violations.Add(ProfileViolation(composePath, "MainEgressMissing", "Main must be attached to the dedicated non-internal egress network."));

        foreach (Match service in Regex.Matches(serviceSection, @"(?ms)^  (?<name>[a-z0-9-]+):\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:|\z)"))
        {
            if (service.Groups["name"].Value != "main" &&
                Regex.IsMatch(service.Groups["body"].Value, @"(?m)^    networks:\s*\[[^\]]*\begress\b[^\]]*\]\s*$"))
                violations.Add(ProfileViolation(composePath, "UnexpectedEgressAttachment", $"Service {service.Groups["name"].Value} must not use the provider egress network."));
        }

        Match egressNetwork = Regex.Match(compose, @"(?ms)^  egress:\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:\s*$|\z)");
        if (!egressNetwork.Success || egressNetwork.Groups["body"].Value.Contains("internal:", StringComparison.Ordinal))
            violations.Add(ProfileViolation(composePath, "EgressNetworkMustBeExternal", "The dedicated provider egress network must be declared without internal=true."));

        Match caddyService = Regex.Match(serviceSection, @"(?ms)^  caddy:\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:\r?$|\z)");
        if (!caddyService.Success ||
            !Regex.IsMatch(caddyService.Groups["body"].Value, @"(?m)^      - ""80:80/tcp""\s*$") ||
            !Regex.IsMatch(caddyService.Groups["body"].Value, @"(?m)^      - ""443:443/tcp""\s*$"))
            violations.Add(ProfileViolation(composePath, "IngressPortAllowlist", "Caddy must publish TCP 80 and TCP 443."));

        return violations;
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
            violations.Add(ProfileViolation(path, "RequiredProfileArtifactMissing", "The evaluated profile artifact is required for source reconciliation."));
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    private static void RequireText(string content, string expected, string path, List<ArchitectureViolation> violations)
    {
        if (!content.Contains(expected, StringComparison.Ordinal))
            violations.Add(ProfileViolation(path, "FrozenBoundaryTextMissing", $"The evaluated owner must contain '{expected}'."));
    }

    private static ArchitectureViolation ProfileViolation(string path, string kind, string remediation) =>
        new(Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace(Path.DirectorySeparatorChar, '/'), 1, kind, "EvaluatedProfile", remediation);

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
}
