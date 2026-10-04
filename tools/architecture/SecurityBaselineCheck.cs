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
        violations.AddRange(RunDeploymentBundles(index.Root));
        violations.AddRange(CheckSourceSecuritySurface(index));
        return violations;
    }

    internal static IReadOnlyList<ArchitectureViolation> RunEndpoints(SourceIndex index) =>
        DiscoverEndpointViolations(index);

    internal static IReadOnlyList<ArchitectureViolation> RunSourceSecuritySurface(SourceIndex index) =>
        CheckSourceSecuritySurface(index);

    internal static IReadOnlyList<ArchitectureViolation> RunDeploymentBundles(string root)
    {
        List<ArchitectureViolation> violations = [];
        string deploymentRoot = Path.Combine(root, "src", "Hosts", "deployment");
        string ingressPath = Path.Combine(deploymentRoot, "docker-compose.ingress.yml");
        string caddyPath = Path.Combine(deploymentRoot, "Caddyfile");
        string caddyDockerfilePath = Path.Combine(deploymentRoot, "Dockerfile");
        string deployScriptPath = Path.Combine(deploymentRoot, "deploy-production.sh");
        string resolverPath = Path.Combine(root, "tools", "ci", "resolve-production-images.mjs");
        string evidencePath = Path.Combine(root, "src", "Hosts", "Web", "scripts", "capture-production-browser-evidence.mjs");
        string ingress = ReadRequired(ingressPath, violations);
        string caddy = ReadRequired(caddyPath, violations);
        string caddyDockerfile = ReadRequired(caddyDockerfilePath, violations);
        string deployScript = ReadRequired(deployScriptPath, violations);
        string resolver = ReadRequired(resolverPath, violations);
        string evidence = ReadRequired(evidencePath, violations);
        string sharedExamplePath = Path.Combine(deploymentRoot, ".env.shared.example");
        string sharedExample = ReadRequired(sharedExamplePath, violations);
        string workflowPath = Path.Combine(root, ".github", "workflows", "production.yml");
        string workflow = ReadRequired(workflowPath, violations);
        Dictionary<string, string> sharedValues = ReadEnvironmentValues(sharedExample, sharedExamplePath, violations);

        if (Regex.Matches(ingress, "\\\"(?:80:80|443:443)/tcp\\\"").Count != 2 ||
            !ingress.Contains("\"80:80/tcp\"", StringComparison.Ordinal) ||
            !ingress.Contains("\"443:443/tcp\"", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(ingressPath, "IngressPortAllowlist", "Publish exactly TCP 80 and TCP 443."));
        if (!Regex.IsMatch(ingress, @"(?ms)^  caddy:\r?\n.*?^      - \.\/Caddyfile:/etc/caddy/Caddyfile:ro\s*$"))
            violations.Add(DeploymentViolation(ingressPath, "CaddyfileMountMissing", "Mount the configured Caddyfile read-only in the runtime service."));
        if (!Regex.IsMatch(caddy, @"(?ms)^\(security_headers\)\s*\{.*?tls\s*\{.*?dns cloudflare \{env\.CLOUDFLARE_API_TOKEN\}\s+resolvers 1\.1\.1\.1\s+\}"))
            violations.Add(DeploymentViolation(caddyPath, "DnsChallengeResolverMissing", "Use the configured public resolver for the Cloudflare DNS challenge."));
        if (!Regex.IsMatch(ingress, @"(?m)^x-caddy-runtime: &caddy-runtime\s*$") ||
            Regex.Matches(ingress, @"(?m)^    <<: \*caddy-runtime\s*$").Count != 2 ||
            !Regex.IsMatch(ingress, @"(?ms)^  caddy-config-validator:\r?\n.*?^    profiles: \[validation\]\s*$") ||
            !Regex.IsMatch(ingress, @"(?ms)^  caddy-config-validator:\r?\n.*?^    network_mode: none\s*$"))
            violations.Add(DeploymentViolation(ingressPath, "CaddyValidationIsolationMissing", "Validate the runtime Caddy configuration without joining fixed-address proxy networks."));
        if (Regex.IsMatch(ingress, @"(?im)443/udp|docker\.sock|agent-runtime"))
            violations.Add(DeploymentViolation(ingressPath, "IngressBoundaryViolation", "Do not enable HTTP/3, mount the Docker socket, or expose the agent runtime."));
        if (!Regex.IsMatch(ingress, @"(?m)^      ingress-egress: \{\}\s*$") ||
            !Regex.IsMatch(ingress, @"(?m)^  test-proxy:\r?\n    name: vitarish-test-proxy\r?\n    internal: true\s*$") ||
            !Regex.IsMatch(ingress, @"(?m)^  production-proxy:\r?\n    name: vitarish-production-proxy\r?\n    internal: true\s*$"))
            violations.Add(DeploymentViolation(ingressPath, "IngressNetworkIsolationMissing", "Keep environment proxy networks internal and ingress egress separately owned."));
        if (!Regex.IsMatch(caddy, @"(?m)^\s*dns cloudflare \{env\.CLOUDFLARE_API_TOKEN\}\s*$") ||
            !Regex.IsMatch(caddyDockerfile, @"(?m)^RUN xcaddy build v2\.10\.0 --with github\.com/caddy-dns/cloudflare\s*$"))
            violations.Add(DeploymentViolation(caddyDockerfilePath, "CloudflareCaddyModuleMissing", "Build the pinned Caddy version with its Cloudflare DNS module."));
        if (!Regex.IsMatch(ingress, @"(?m)^    CLOUDFLARE_API_TOKEN: \$\{VITARISH_CLOUDFLARE_API_TOKEN:\?[^}]+\}\s*$") ||
            !sharedValues.ContainsKey("VITARISH_CLOUDFLARE_API_TOKEN") ||
            !sharedValues.ContainsKey("VITARISH_ACME_EMAIL"))
            violations.Add(DeploymentViolation(sharedExamplePath, "IngressSecretInputMissing", "Require the Cloudflare DNS credential and ACME contact through the shared operator environment."));
        foreach (string required in new[] { "VITARISH_CADDY_IMAGE", "VITARISH_POSTGRES_IMAGE", "VITARISH_REDIS_IMAGE", "VITARISH_AISTOR_IMAGE", "VITARISH_ELASTICSEARCH_IMAGE" })
        {
            if (!sharedValues.ContainsKey(required))
                violations.Add(DeploymentViolation(sharedExamplePath, "SharedImageInputMissing", $"Declare the shared image input {required}."));
        }
        if (!sharedValues.TryGetValue("VITARISH_POSTGRES_IMAGE", out string? postgresImage) ||
            !Regex.IsMatch(postgresImage, @"^postgres:(?:1[89]|[2-9][0-9])(?:\.\d+)?$"))
            violations.Add(DeploymentViolation(sharedExamplePath, "PostgresVolumeLayoutMismatch", "Use PostgreSQL 18 or later with the versioned parent-volume mount."));
        if (!Regex.IsMatch(sharedExample, @"(?m)^VITARISH_AISTOR_IMAGE=quay\.io/minio/aistor/minio@sha256:0{64}$"))
            violations.Add(DeploymentViolation(sharedExamplePath, "AistorImageNotDigestPinned", "Declare the licensed AIStor image by immutable digest."));
        if (!sharedValues.TryGetValue("VITARISH_DEFAULT_RESPONSE_MODEL", out string? responseModel) || string.IsNullOrWhiteSpace(responseModel))
            violations.Add(DeploymentViolation(sharedExamplePath, "SharedModelInputMissing", "Declare the shared default intelligence response model."));
        if (!workflow.Contains("dorny/paths-filter@v4", StringComparison.Ordinal) ||
            !workflow.Contains("ghcr.io/${{ github.repository_owner }}/vitarish/${{ matrix.image }}:${{ github.sha }}", StringComparison.Ordinal) ||
            !workflow.Contains("packages: write", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(workflowPath, "ProductionWorkflowImageContractInvalid", "Filter affected consumers and publish images under their full commit SHA in the established GHCR namespace."));
        if (!workflow.Contains("predicate-quantifier: some-with-excludes", StringComparison.Ordinal) ||
            !workflow.Contains("unknown_source:", StringComparison.Ordinal) ||
            !workflow.Contains("'!src/Hosts/Main/**'", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(workflowPath, "ProductionPathFilterExclusionMissing", "Exclude classified source paths before choosing the broad fallback matrix."));
        int publishStart = workflow.IndexOf("\n  publish:", StringComparison.Ordinal);
        int deployStart = workflow.IndexOf("\n  deploy_production:", StringComparison.Ordinal);
        int packageWrite = workflow.IndexOf("packages: write", StringComparison.Ordinal);
        if (!Regex.IsMatch(workflow, @"(?m)^  pull_request:\s*$") ||
            !Regex.IsMatch(workflow, @"(?m)^  push:\s*$") ||
            !workflow.Contains("workflow_dispatch:", StringComparison.Ordinal) ||
            workflow.Contains("schedule:", StringComparison.Ordinal) ||
            !Regex.IsMatch(workflow, @"(?ms)^permissions:\r?\n  contents: read\r?\n  pull-requests: read\s*$") ||
            publishStart < 0 || deployStart <= publishStart || packageWrite <= publishStart || packageWrite >= deployStart ||
            !Regex.IsMatch(workflow[publishStart..deployStart], @"(?ms)^    permissions:\r?\n      contents: read\r?\n      packages: write\s*$"))
            violations.Add(DeploymentViolation(workflowPath, "ProductionWorkflowPermissionsInvalid", "Keep PR permissions read-only and grant package write only to publication on main pushes."));
        if (!Regex.IsMatch(workflow, @"(?ms)^  publish:\r?\n(?:(?!^  [a-z_]+:).)*?^    needs: \[changes, required\].*?^    if: \$\{\{ github\.event_name == 'push'.*?github\.ref == 'refs/heads/main'.*?needs\.required\.result == 'success'.*?\}\}") ||
            !Regex.IsMatch(workflow, @"(?ms)^  deploy_production:\r?\n(?:(?!^  [a-z_]+:).)*?^    needs: \[changes, required, publish\].*?^    if: \$\{\{ always\(\) && !cancelled\(\) && github\.event_name == 'push'.*?github\.ref == 'refs/heads/main'.*?needs\.required\.result == 'success'.*?\}\}") ||
            !HasCompleteRequiredCheckDependencies(workflow) ||
            !Regex.IsMatch(workflow, @"(?ms)^  required:\r?\n(?:(?!^  [a-z_]+:).)*?^    if: \$\{\{ always\(\) \}\}") ||
            !workflow.Contains("contains(needs.*.result, 'failure')", StringComparison.Ordinal) ||
            !workflow.Contains("contains(needs.*.result, 'cancelled')", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(workflowPath, "ProductionWorkflowTestGateMissing", "Make both publication and SSH deployment depend on successful affected checks and main pushes."));
        if (!workflow.Contains("environment: production", StringComparison.Ordinal) ||
            !workflow.Contains("secrets.PRODUCTION_SSH_HOST", StringComparison.Ordinal) ||
            !workflow.Contains("secrets.PRODUCTION_SSH_USER", StringComparison.Ordinal) ||
            !workflow.Contains("secrets.PRODUCTION_SSH_PRIVATE_KEY", StringComparison.Ordinal) ||
            !workflow.Contains("secrets.PRODUCTION_SSH_KNOWN_HOSTS", StringComparison.Ordinal) ||
            !workflow.Contains("[[ \"$SSH_USER\" == \"root\" &&", StringComparison.Ordinal) ||
            !workflow.Contains("StrictHostKeyChecking=yes", StringComparison.Ordinal) ||
            !workflow.Contains("group: vitarish-production-deploy", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(workflowPath, "ProductionSshBoundaryInvalid", "Scope pinned SSH access to the production environment and serialize production deployments."));
        int rollbackTrap = deployScript.IndexOf("trap rollback ERR", StringComparison.Ordinal);
        int firstReplacement = deployScript.IndexOf("install -m 0600 \"$incoming/release.env\"", StringComparison.Ordinal);
        if (!deployScript.Contains("config --quiet", StringComparison.Ordinal) ||
            !deployScript.Contains("caddy validate --config /etc/caddy/Caddyfile", StringComparison.Ordinal) ||
            !deployScript.Contains("--no-deps --pull never", StringComparison.Ordinal) ||
            !workflow.Contains("READINESS_PATH", StringComparison.Ordinal) ||
            !deployScript.Contains("compose_previous up", StringComparison.Ordinal) ||
            !deployScript.Contains("cp -a \"$backup/$file\"", StringComparison.Ordinal) ||
            rollbackTrap < 0 || firstReplacement < 0 || rollbackTrap >= firstReplacement)
            violations.Add(DeploymentViolation(deployScriptPath, "ProductionWorkflowRecoveryGateMissing", "Install rollback before replacing active configuration, validate protected Compose inputs, wait for health, and restore previous references on failure."));
        if (!deployScript.Contains("dependencies=(postgres redis aistor elasticsearch)", StringComparison.Ordinal) ||
            !deployScript.Contains("ingress up -d --wait", StringComparison.Ordinal) ||
            !deployScript.Contains("compose up -d --wait --wait-timeout 600 --no-deps --no-recreate --pull never \"${dependencies[@]}\"", StringComparison.Ordinal) ||
            !deployScript.Contains("compose config --services", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(deployScriptPath, "ProductionBootstrapMissing", "Start ingress, stateful dependencies, and missing application services before probing a new host."));
        if (!workflow.Contains("docker buildx imagetools inspect \"$image_ref\"", StringComparison.Ordinal) ||
            !workflow.Contains("actions/download-artifact@v4", StringComparison.Ordinal) ||
            !resolver.Contains("docker", StringComparison.Ordinal) ||
            !resolver.Contains("imagetools", StringComparison.Ordinal) ||
            !resolver.Contains("publishedDigest !== registryDigest", StringComparison.Ordinal) ||
            !deployScript.Contains("@sha256:", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(resolverPath, "ProductionImageDigestGateMissing", "Deploy the registry digest published by this workflow run and reject a moved SHA tag."));
        if (!deployScript.Contains(".workflow-deployed.json", StringComparison.Ordinal) ||
            !deployScript.Contains("'commit': plan['commit']", StringComparison.Ordinal) ||
            !evidence.Contains("record.get('commit') != commit", StringComparison.Ordinal) ||
            !evidence.Contains("image != configured['image']", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(evidencePath, "ProductionReleaseEvidenceIdentityMissing", "Bind release evidence to the workflow commit and exact running Compose image references, including configuration-only runs."));
        if (!workflow.Contains("cache-from: type=gha", StringComparison.Ordinal) ||
            !workflow.Contains("cache-to: type=gha", StringComparison.Ordinal) ||
            !workflow.Contains("secret-envs: HEROUI_AUTH_TOKEN=HEROUI_AUTH_TOKEN", StringComparison.Ordinal))
            violations.Add(DeploymentViolation(workflowPath, "WebImageBuildInputsMissing", "Use BuildKit cache and the existing Web build credential for Web image builds."));

        string[] routeNames = ["PUBLIC", "PATIENTS", "PLATFORM_ADMIN", "PM", "KNOWLEDGE_QA", "LANDING"];
        string[] environmentPrefixes = ["TEST", "PRODUCTION"];
        foreach (string prefix in environmentPrefixes)
        {
            foreach (string route in routeNames)
            {
                string variable = $"{prefix}_{route}_HOST";
                if (!ingress.Contains(variable, StringComparison.Ordinal) || !caddy.Contains("{$" + variable + "}", StringComparison.Ordinal))
                    violations.Add(DeploymentViolation(caddyPath, "RouteHostMissing", $"Wire the {variable} input through ingress and Caddy."));
            }
        }

        foreach (string environment in new[] { "test", "production" })
        {
            string composePath = Path.Combine(deploymentRoot, $"docker-compose.{environment}.yml");
            string compose = ReadRequired(composePath, violations);
            string examplePath = Path.Combine(deploymentRoot, $".env.{environment}.example");
            string example = ReadRequired(examplePath, violations);
            Dictionary<string, string> exampleValues = ReadEnvironmentValues(example, examplePath, violations);
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
            if (!Regex.IsMatch(compose, @"(?m)^      ASPNETCORE_ENVIRONMENT: Production\s*$") ||
                !Regex.IsMatch(compose, @"(?m)^      Vitarish__Security__Tls__RequireHttps: ""true""\s*$"))
                violations.Add(DeploymentViolation(composePath, "MainProductionTlsPolicyMissing", "Main must run in Production and require HTTPS."));
            if (!Regex.IsMatch(compose, @"(?m)^      Vitarish__Intelligence__DefaultResponseModel: \$\{VITARISH_DEFAULT_RESPONSE_MODEL:\?[^}]+\}\s*$"))
                violations.Add(DeploymentViolation(composePath, "MainModelInputMissing", "Main must receive the shared response model through its required input."));
            if (environment == "production" && !Regex.IsMatch(compose, @"(?ms)^  main:\r?\n.*?^    env_file:\r?\n      - path: \.\/\.env\.production\.mediatr\r?\n        required: false\s*$"))
                violations.Add(DeploymentViolation(composePath, "ProductionMediatREnvironmentMissing", "Load the optional protected production MediatR environment file."));
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
            if (!Regex.IsMatch(compose, $@"(?m)^  application: \{{ name: vitarish-{environment}-application, internal: true \}}$") ||
                !Regex.IsMatch(compose, $@"(?m)^  proxy: \{{ name: vitarish-{environment}-proxy, external: true \}}$"))
                violations.Add(DeploymentViolation(composePath, "EnvironmentNetworkOwnershipMissing", "Use the environment-owned private application network and its shared external ingress network."));
            foreach (string ownedResource in new[] { "application", "proxy", "postgres", "redis", "aistor", "elasticsearch" })
            {
                string expected = $"vitarish-{environment}-{ownedResource}";
                if (!compose.Contains(expected, StringComparison.Ordinal))
                    violations.Add(DeploymentViolation(composePath, "EnvironmentResourceOwnershipMissing", $"Declare an environment-owned network or volume named {expected}."));
            }
            foreach (Match requiredInput in Regex.Matches(compose, @"\$\{(?<name>VITARISH_[A-Z0-9_]+):\?"))
            {
                string name = requiredInput.Groups["name"].Value;
                if (!sharedValues.ContainsKey(name) && !exampleValues.ContainsKey(name))
                    violations.Add(DeploymentViolation(examplePath, "ComposeInputUndeclared", $"Declare required Compose input {name} in exactly one committed environment example."));
                if (sharedValues.ContainsKey(name) && exampleValues.ContainsKey(name))
                    violations.Add(DeploymentViolation(examplePath, "ComposeInputOwnershipDuplicated", $"Give {name} exactly one committed environment owner."));
            }
            if (!Regex.IsMatch(compose, $@"(?m)^      Vitarish__Security__TrustedProxyNetworks__0: \$\{{VITARISH_{environment.ToUpperInvariant()}_INGRESS_IPV4:\?[^}}]+\}}/32\s*$"))
                violations.Add(DeploymentViolation(composePath, "TrustedProxyRangeTooBroad", "Trust only the environment's exact Caddy ingress IPv4 address."));
            if (!Regex.IsMatch(compose, $@"(?m)^    volumes: \[{environment}-postgres:/var/lib/postgresql\]\s*$"))
                violations.Add(DeploymentViolation(composePath, "PostgresVolumeLayoutMismatch", "Persist the PostgreSQL versioned data directory through its environment-owned parent volume."));
            if (!Regex.IsMatch(compose, @"(?m)^    command: server /mnt/data --license /minio\.license\s*$") ||
                !Regex.IsMatch(compose, @"(?m)^        source: \.\/minio\.license\s*$") ||
                !Regex.IsMatch(compose, @"(?m)^        target: /minio\.license\s*$") ||
                !Regex.IsMatch(compose, @"(?m)^        read_only: true\s*$"))
                violations.Add(DeploymentViolation(composePath, "AistorLicenseMountUnsafe", "Mount the licensed AIStor file read-only at its required container path."));
            foreach (string route in routeNames)
            {
                string variable = $"VITARISH_{environment.ToUpperInvariant()}_{route}_HOST";
                if (!compose.Contains(variable, StringComparison.Ordinal) || !caddy.Contains("{$" + variable["VITARISH_".Length..] + "}", StringComparison.Ordinal))
                    violations.Add(DeploymentViolation(composePath, "RouteHostBindingMissing", $"Wire the {variable} input through the Compose and Caddy configurations."));
            }
            string environmentPrefix = environment.ToUpperInvariant();
            foreach (string wildcard in new[] { "WILDCARD_HOST", "CLINIC_WILDCARD_HOST" })
            {
                string variable = $"{environmentPrefix}_{wildcard}";
                string binding = Regex.Escape("{$" + variable + "}");
                if (!ingress.Contains($"{variable}: ${{VITARISH_{variable}:?", StringComparison.Ordinal) ||
                    !Regex.IsMatch(caddy, @"(?ms)^http://" + binding + @"\s*\{\s*redir https://\{host\}\{uri\} 308\s*\}") ||
                    !Regex.IsMatch(caddy, @"(?ms)^https://" + binding + @"\s*\{\s*import security_headers\s+import " + environment + @"_api\s+reverse_proxy " + environment.ToLowerInvariant() + @"-private-practice-owner:3000\s*\}"))
                    violations.Add(DeploymentViolation(caddyPath, "WildcardRouteSecurityMissing", $"Redirect {variable} to HTTPS and route it through Main API and the owner Web service."));
            }
            Dictionary<string, string[]> routeTargets = new(StringComparer.Ordinal)
            {
                ["PUBLIC"] = [$"{environment}-private-practice-owner:3000"],
                ["PATIENTS"] = [$"{environment}-patients:3001"],
                ["PLATFORM_ADMIN"] = [$"{environment}-platform-admin:3003"],
                ["PM"] = [$"{environment}-pm:3004"],
                ["KNOWLEDGE_QA"] = [$"{environment}-knowledge-qa:3001"],
                ["LANDING"] = [$"{environment}-landing:3002"],
            };
            foreach ((string route, string[] targets) in routeTargets)
            {
                string variable = $"{environmentPrefix}_{route}_HOST";
                string hostBinding = Regex.Escape("{$" + variable + "}");
                string hostBlock = Regex.Match(caddy, @"(?ms)^https://" + hostBinding + @"\s*\{(?<body>.*?)^\}").Groups["body"].Value;
                if (string.IsNullOrWhiteSpace(hostBlock) || targets.Any(target => !Regex.IsMatch(hostBlock, @"(?m)^\s*reverse_proxy[^\r\n]*" + Regex.Escape(target) + @"\s*$")))
                    violations.Add(DeploymentViolation(caddyPath, "IngressRouteTargetMissing", $"Route {variable} only to its declared internal Compose services."));
            }
            Match agentRuntime = Regex.Match(compose, @"(?ms)^  agent-runtime:\r?\n(?<body>.*?)(?=^  postgres:\r?\n)");
            string agentBody = agentRuntime.Groups["body"].Value;
            if (!agentRuntime.Success ||
                !agentBody.Contains($"VITARISH_{environmentPrefix}_OPENAI_API_KEY:?", StringComparison.Ordinal) ||
                !agentBody.Contains("AGENT_RUNTIME_SHARED_SECRET", StringComparison.Ordinal) ||
                !Regex.IsMatch(agentBody, @"(?m)^    networks: \[application, egress\]\s*$"))
                violations.Add(DeploymentViolation(composePath, "AgentRuntimeBoundaryMissing", "Require the provider key and shared secret and confine the agent runtime to application and provider-egress networks."));
        }

        if (Regex.IsMatch(ingress, @"(?im)^\s*image:\s*[^\r\n]*:latest\s*$"))
            violations.Add(DeploymentViolation(ingressPath, "MutableImageReference", "Use a versioned or digest-pinned ingress image."));
        if (Regex.IsMatch(caddy, @"(?im)reverse_proxy[^\r\n]*agent-runtime"))
            violations.Add(DeploymentViolation(caddyPath, "PublicAgentRuntime", "The server-only agent runtime must remain private."));
        foreach (string environment in environmentPrefixes)
        {
            string apiImport = environment.ToLowerInvariant() + "_api";
            if (Regex.Matches(caddy, @"(?m)^\s+import " + Regex.Escape(apiImport) + @"\s*$").Count != 8 ||
                !Regex.IsMatch(caddy, @"(?ms)^\(" + Regex.Escape(apiImport) + @"\)\s*\{\s*@api path /api/\*\s*reverse_proxy @api " + environment.ToLowerInvariant() + @"-main:8080\s*\}"))
                violations.Add(DeploymentViolation(caddyPath, "ApiRouteCoverageMissing", $"Route same-origin API requests from all application hosts to {environment.ToLowerInvariant()} Main."));
        }

        return violations;
    }

    private static bool HasCompleteRequiredCheckDependencies(string workflow)
    {
        Match jobsStart = Regex.Match(workflow, @"(?m)^jobs:[ \t]*\r?\n");
        if (!jobsStart.Success) return false;

        string jobDefinitions = workflow[(jobsStart.Index + jobsStart.Length)..];
        MatchCollection jobMatches = Regex.Matches(
            jobDefinitions,
            @"(?ms)^  (?<id>[a-z][a-z0-9_-]*):\r?\n(?<body>.*?)(?=^  [a-z][a-z0-9_-]*:|\z)");
        Dictionary<string, string[]> needsByJob = new(StringComparer.Ordinal);
        foreach (Match job in jobMatches)
        {
            Match needs = Regex.Match(job.Groups["body"].Value, @"(?m)^    needs:[ \t]*(?<value>[^\r\n]+)(?=\r?$)");
            if (!needs.Success) continue;

            string[]? dependencies = ParseWorkflowNeeds(needs.Groups["value"].Value);
            if (dependencies is null || !needsByJob.TryAdd(job.Groups["id"].Value, dependencies)) return false;
        }

        if (!needsByJob.TryGetValue("required", out string[]? requiredNeeds) || requiredNeeds is null) return false;

        HashSet<string> requiredDependencies = new(requiredNeeds, StringComparer.Ordinal);
        HashSet<string> checkJobsAndPrerequisites = new(StringComparer.Ordinal);
        foreach ((string jobId, string[] dependencies) in needsByJob)
        {
            if (jobId == "required" || dependencies.Length != 1) continue;
            checkJobsAndPrerequisites.Add(jobId);
            checkJobsAndPrerequisites.UnionWith(dependencies);
        }

        return checkJobsAndPrerequisites.Count > 0 &&
            checkJobsAndPrerequisites.All(requiredDependencies.Contains);
    }

    private static string[]? ParseWorkflowNeeds(string value)
    {
        value = value.Trim();
        if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
        {
            value = value[1..^1].Trim();
            if (value.Length == 0) return [];

            string[] dependencies = value.Split(',', StringSplitOptions.TrimEntries);
            return dependencies.All(IsWorkflowJobId) ? dependencies : null;
        }

        return IsWorkflowJobId(value) ? [value] : null;
    }

    private static bool IsWorkflowJobId(string value) =>
        Regex.IsMatch(value, @"^[a-z][a-z0-9_-]*$", RegexOptions.CultureInvariant);

    private static Dictionary<string, string> ReadEnvironmentValues(string content, string path, List<ArchitectureViolation> violations)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            Match match = Regex.Match(line, @"^(?<name>[A-Z0-9_]+)=(?<value>.*)$");
            if (!match.Success) continue;
            string name = match.Groups["name"].Value;
            if (!values.TryAdd(name, match.Groups["value"].Value))
                violations.Add(DeploymentViolation(path, "EnvironmentInputDuplicate", $"Declare environment input {name} only once in its committed example."));
        }
        return values;
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
