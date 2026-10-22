using System.Text.RegularExpressions;

namespace Vitarish.Tools.Architecture;

internal static class SelfTests
{
    public static void Run(string repositoryRoot)
    {
        InternalModuleClientCheckSelfTest();
        OpenApiEndpointCheckSelfTest();
        SecurityBaselineSelfTest();
        DeploymentBundleSecuritySelfTest(repositoryRoot);
        Console.WriteLine("Architecture Roslyn self-tests passed");
    }

    private static void DeploymentBundleSecuritySelfTest(string repositoryRoot)
    {
        string fixtureRoot = Path.Combine(Path.GetTempPath(), "vitarish-security-baseline-" + Guid.NewGuid().ToString("N"));
        string[] files =
        [
            "src/Hosts/deployment/Caddyfile",
            "src/Hosts/deployment/Dockerfile",
            "src/Hosts/deployment/docker-compose.ingress.yml",
            "src/Hosts/deployment/docker-compose.test.yml",
            "src/Hosts/deployment/docker-compose.production.yml",
            "src/Hosts/deployment/.env.shared.example",
            "src/Hosts/deployment/.env.test.example",
            "src/Hosts/deployment/.env.production.example",
            ".github/workflows/production.yml",
            "src/Hosts/deployment/deploy-production.sh",
            "tools/ci/resolve-production-images.mjs",
            "src/Hosts/Web/scripts/capture-production-browser-evidence.mjs",
        ];
        try
        {
            Dictionary<string, string> sourceFiles = [];
            foreach (string relativePath in files)
            {
                string sourcePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string fixturePath = Path.Combine(fixtureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fixturePath)!);
                File.Copy(sourcePath, fixturePath);
                sourceFiles[relativePath] = File.ReadAllText(sourcePath);
            }

            IReadOnlyList<ArchitectureViolation> positiveViolations = SecurityBaselineCheck.RunDeploymentBundles(fixtureRoot);
            Require(positiveViolations.Count == 0,
                "valid deployment configuration produced a security-baseline violation: " + string.Join(", ", positiveViolations.Select(violation => violation.Kind)));
            (string File, string Pattern, string Replacement, string Kind)[] negatives =
            [
                (files[2], Regex.Escape("\"443:443/tcp\""), "\"443:443/udp\"", "IngressBoundaryViolation"),
                (files[2], Regex.Escape("\"80:80/tcp\""), "\"8080:80/tcp\"", "IngressPortAllowlist"),
                (files[2], @"(?m)^\s+- \.\/Caddyfile:/etc/caddy/Caddyfile:ro\s*$", "", "CaddyfileMountMissing"),
                (files[0], "resolvers 1\\.1\\.1\\.1", "resolvers 8.8.8.8", "DnsChallengeResolverMissing"),
                (files[2], "network_mode: none", "network_mode: host", "CaddyValidationIsolationMissing"),
                (files[2], "ingress-egress: {}", "ingress-egress: internal: true", "IngressNetworkIsolationMissing"),
                (files[5], "VITARISH_ACME_EMAIL=", "VITARISH_ACME_CONTACT=", "IngressSecretInputMissing"),
                (files[1], "v2\\.10\\.0", "v2.9.0", "CloudflareCaddyModuleMissing"),
                (files[5], @"(?m)^VITARISH_REDIS_IMAGE=.*(?:\r?\n)?", "", "SharedImageInputMissing"),
                (files[5], "VITARISH_POSTGRES_IMAGE=postgres:18\\.0", "VITARISH_POSTGRES_IMAGE=postgres:17.0", "PostgresVolumeLayoutMismatch"),
                (files[5], "VITARISH_AISTOR_IMAGE=quay\\.io/minio/aistor/minio@sha256:0{64}", "VITARISH_AISTOR_IMAGE=quay.io/minio/aistor/minio:latest", "AistorImageNotDigestPinned"),
                (files[5], @"(?m)^VITARISH_DEFAULT_RESPONSE_MODEL=.*$", "VITARISH_DEFAULT_RESPONSE_MODEL=", "SharedModelInputMissing"),
                (files[8], "dorny/paths-filter@v4", "dorny/paths-filter@v3", "ProductionWorkflowImageContractInvalid"),
                (files[8], "predicate-quantifier: some-with-excludes", "predicate-quantifier: some", "ProductionPathFilterExclusionMissing"),
                (files[0], Regex.Escape("{$PRODUCTION_PM_HOST}"), "{$PRODUCTION_UNKNOWN_HOST}", "RouteHostMissing"),
                (files[8], "packages: write", "contents: read", "ProductionWorkflowPermissionsInvalid"),
                (files[8], "pull-requests: read", "actions: read", "ProductionWorkflowPermissionsInvalid"),
                (files[8], "needs.required.result == 'success'", "needs.required.result == 'always'", "ProductionWorkflowTestGateMissing"),
                (files[8], "needs.publish.result == 'success'", "needs.required.result == 'success'", "ProductionWorkflowTestGateMissing"),
                (files[8], Regex.Escape("needs: [changes, dotnet, web, web_shared_tests, authentication_web, security]"), "needs: [changes, dotnet, web, web_shared_tests, authentication_web]", "ProductionWorkflowTestGateMissing"),
                (files[8], Regex.Escape("needs: [changes, dotnet, web, web_shared_tests, authentication_web, security]"), "needs: [changes, dotnet, web, web_shared_tests, security]", "ProductionWorkflowTestGateMissing"),
                (files[8], Regex.Escape("github.event_name == 'workflow_dispatch' && github.ref == 'refs/heads/main' && needs.changes.outputs.image_matrix"), "github.event_name == 'push' && github.ref == 'refs/heads/main' && needs.changes.outputs.image_matrix", "ProductionWorkflowTestGateMissing"),
                (files[8], Regex.Escape("!cancelled() && github.event_name == 'workflow_dispatch'"), "!cancelled() && github.event_name == 'push'", "ProductionWorkflowTestGateMissing"),
                (files[8], @"(?ms)(^  required:\r?\n(?:(?!^  [a-z_]+:).)*?^    needs: )\[[^\]]*\]", "$1[]", "ProductionWorkflowTestGateMissing"),
                (files[8], "github.ref == 'refs/heads/main'", "github.ref == 'refs/heads/release'", "ProductionWorkflowTestGateMissing"),
                (files[8], "always\\(\\) && !cancelled\\(\\) && github.event_name", "github.event_name", "ProductionWorkflowTestGateMissing"),
                (files[8], "StrictHostKeyChecking=yes", "StrictHostKeyChecking=no", "ProductionSshBoundaryInvalid"),
                (files[8], Regex.Escape("[[ \"$SSH_USER\" == \"root\" &&"), "[[ \"$SSH_USER\" =~", "ProductionSshBoundaryInvalid"),
                (files[9], "trap rollback ERR", ":", "ProductionWorkflowRecoveryGateMissing"),
                (files[9], "dependencies=\\(postgres redis aistor elasticsearch\\)", "dependencies=()", "ProductionBootstrapMissing"),
                (files[10], "publishedDigest !== registryDigest", "publishedDigest === registryDigest", "ProductionImageDigestGateMissing"),
                (files[8], Regex.Escape("docker buildx imagetools inspect \"$image_ref\""), "echo", "ProductionImageDigestGateMissing"),
                (files[11], "record.get\\('commit'\\) != commit", "record.get('commit') == commit", "ProductionReleaseEvidenceIdentityMissing"),
                (files[8], "secret-envs: HEROUI_AUTH_TOKEN=HEROUI_AUTH_TOKEN", "secret-envs: OTHER_TOKEN=OTHER_TOKEN", "WebImageBuildInputsMissing"),
                (files[4], "Vitarish__Security__Tls__RequireHttps: \\\"true\\\"", "Vitarish__Security__Tls__RequireHttps: \\\"false\\\"", "MainProductionTlsPolicyMissing"),
                (files[4], "Vitarish__Intelligence__DefaultResponseModel", "Vitarish__Intelligence__ResponseModel", "MainModelInputMissing"),
                (files[4], @"(?ms)^    env_file:\r?\n      - path: \.\/\.env\.production\.mediatr\r?\n        required: false\r?\n", "", "ProductionMediatREnvironmentMissing"),
                (files[4], "VITARISH_DEFAULT_RESPONSE_MODEL", "VITARISH_UNDECLARED_MODEL", "ComposeInputUndeclared"),
                (files[5], @"$", Environment.NewLine + "VITARISH_PRODUCTION_MAIN_IMAGE=ghcr.io/example/vitarish/main:example", "ComposeInputOwnershipDuplicated"),
                (files[4], @"(?m)^      Vitarish__Security__TrustedProxyNetworks__0:.*$", "      Vitarish__Security__TrustedProxyNetworks__0: 0.0.0.0/0", "TrustedProxyRangeTooBroad"),
                (files[4], "vitarish-production-application, internal: true", "vitarish-wrong-application, internal: false", "EnvironmentNetworkOwnershipMissing"),
                (files[4], @"(?ms)(source: \.\/minio\.license\s*\n\s+target: /minio\.license\s*\n\s+read_only: )true", "$1false", "AistorLicenseMountUnsafe"),
                (files[4], "VITARISH_PRODUCTION_PM_HOST", "VITARISH_PRODUCTION_UNKNOWN_HOST", "RouteHostBindingMissing"),
                (files[0], @"(?m)^\s+import production_api\s*$", "", "ApiRouteCoverageMissing"),
                (files[0], "reverse_proxy production-platform-admin:3003", "reverse_proxy wrong-service:3003", "IngressRouteTargetMissing"),
                (files[0], "redir https://{host}{uri} 308", "redir http://{host}{uri} 308", "WildcardRouteSecurityMissing"),
                (files[4], "VITARISH_PRODUCTION_OPENROUTER_API_KEY", "VITARISH_WRONG_PROVIDER_API_KEY", "AgentRuntimeBoundaryMissing"),
                (files[4], "OPENAI_BASE_URL: https://openrouter.ai/api/v1", "OPENAI_BASE_URL: https://provider.example.invalid/v1", "AgentRuntimeBoundaryMissing"),
                (files[5], @"$", Environment.NewLine + "VITARISH_ACME_EMAIL=duplicate@example.invalid", "EnvironmentInputDuplicate"),
                (files[3], @"(?m)^  postgres:\s*$", "  database:", "RequiredServiceMissing"),
                (files[4], Regex.Escape("\"${VITARISH_PRODUCTION_MAIN_IMAGE:?VITARISH_PRODUCTION_MAIN_IMAGE is required}\""), "nginx:latest", "MutableImageReference"),
                (files[4], @"(?m)^  main:\s*$", "  main:" + Environment.NewLine + "    ports:" + Environment.NewLine + "      - \"8080:8080\"", "PublishedEnvironmentPort"),
                (files[4], @"(?m)^    healthcheck:.*$", "    command: /bin/false", "HealthcheckMissing"),
            ];
            foreach ((string relativePath, string pattern, string replacement, string kind) in negatives)
            {
                foreach (string file in files)
                    File.WriteAllText(Path.Combine(fixtureRoot, file.Replace('/', Path.DirectorySeparatorChar)), sourceFiles[file]);
                string fixturePath = Path.Combine(fixtureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string original = sourceFiles[relativePath];
                string mutated = Regex.Replace(original, pattern, replacement, RegexOptions.CultureInvariant);
                Require(mutated != original, $"negative deployment fixture for {kind} did not change its source");
                File.WriteAllText(fixturePath, mutated);
                IReadOnlyList<ArchitectureViolation> violations = SecurityBaselineCheck.RunDeploymentBundles(fixtureRoot);
                Require(violations.Any(violation => violation.Kind == kind), $"deployment security regression fixture did not reject {kind} for {relativePath} pattern {pattern}");
            }
            Console.WriteLine("Deployment security baseline fixtures passed");
        }
        finally
        {
            if (Directory.Exists(fixtureRoot)) Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    private static void InternalModuleClientCheckSelfTest()
    {
        SourceIndex unsafeIndex = SourceIndex.FromSources(
            ("src/Modules/Base/Client/AuthenticatedModuleApiClient.cs", "namespace Vitarish.Modules.Base.Client; public abstract class AuthenticatedModuleApiClient(System.Net.Http.HttpClient client) { }"),
            ("src/Modules/Foo/Client/UnsafeClient.cs", "using System.Net.Http; namespace Foo; public sealed class UnsafeClient(HttpClient client) { }"),
            ("src/Modules/Foo/Client/SafeClient.cs", "using System.Net.Http; using Vitarish.Modules.Base.Client; namespace Foo; public sealed class SafeClient(HttpClient client) : AuthenticatedModuleApiClient(client) { }"),
            ("src/Modules/Foo/Registration.cs", "using Microsoft.Extensions.DependencyInjection; namespace Foo; static class Registration { static void Add(IServiceCollection services) => services.AddHttpClient<UnsafeClient>(); }"));

        IReadOnlyList<ArchitectureViolation> violations = InternalModuleClientCheck.Run(unsafeIndex);
        Require(violations.Any(violation => violation.Kind == "UnauthenticatedModuleClient"), "unsafe client was not detected");
        Require(violations.Any(violation => violation.Kind == "DirectModuleClientRegistration"), "direct registration was not detected");

        SourceIndex safeIndex = SourceIndex.FromSources(
            ("src/Modules/Base/Client/AuthenticatedModuleApiClient.cs", "namespace Vitarish.Modules.Base.Client; public abstract class AuthenticatedModuleApiClient(System.Net.Http.HttpClient client) { }"),
            ("src/Modules/Foo/Client/SafeClient.cs", "using System.Net.Http; using Vitarish.Modules.Base.Client; namespace Foo; public sealed class SafeClient(HttpClient client) : AuthenticatedModuleApiClient(client) { }"),
            ("src/Modules/Foo/Registration.cs", "namespace Foo; static class Registration { static void Add() { } }"));
        Require(InternalModuleClientCheck.Run(safeIndex).Count == 0, "safe client produced a violation");
        Console.WriteLine("Internal module client Roslyn self-test passed");
    }

    private static void OpenApiEndpointCheckSelfTest()
    {
        SourceIndex validIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Contracts/FooContracts.cs", "namespace Foo.Contracts; public sealed record ResponseDto; public sealed record RequestDto;"),
            ("src/Modules/Foo/Api/FooApi.cs", """
                using Foo.Contracts;
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object group) => group.MapModulePost(
                        "/items",
                        CreateAsync,
                        "FooCreateItem",
                        typeof(ResponseDto));
                    static void CreateAsync(RequestDto request) { }
                }
                """));
        Require(OpenApiEndpointCheck.Run(validIndex, validateAuthorizationMatrix: false).Count == 0, "valid OpenAPI endpoint produced a violation");

        SourceIndex namedOperationIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Contracts/FooContracts.cs", "namespace Foo.Contracts; public sealed record ResponseDto;"),
            ("src/Modules/Foo/Api/FooApi.cs", """
                using Foo.Contracts;
                namespace Foo.Api;
                static class FooApi
                {
                    private const string OperationId = "FooGetItems";
                    static void Map(object group) => group.MapModuleGet("/items", GetAsync, OperationId, typeof(ResponseDto));
                    static void GetAsync() { }
                }
                """));
        Require(OpenApiEndpointCheck.Run(namedOperationIndex, validateAuthorizationMatrix: false).Count == 0, "named operation ID produced a violation");

        SourceIndex namedJustificationIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    private const string Justification = "Public images.";
                    static void Map(object endpoints) => endpoints.MapAnonymousModuleGroup("/api/foo/public", Justification);
                }
                """));
        Require(OpenApiEndpointCheck.Run(namedJustificationIndex, validateAuthorizationMatrix: false).Count == 0, "named anonymous justification produced a violation");

        SourceIndex unresolvedJustificationIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints) => endpoints.MapAnonymousModuleGroup("/api/foo/public", Justification());
                    static string Justification() => "Public images.";
                }
                """));
        Require(OpenApiEndpointCheck.Run(unresolvedJustificationIndex, validateAuthorizationMatrix: false).Any(violation => violation.Kind == "AnonymousJustificationMissing"), "unresolvable anonymous justification was not rejected");

        SourceIndex invalidIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", "namespace Foo.Api; static class FooApi { static void Map(object group) => group.MapGet(\"/items\", () => 1); }"));
        Require(OpenApiEndpointCheck.Run(invalidIndex, validateAuthorizationMatrix: false).Any(violation => violation.Kind == "RawEndpointMapping"), "raw endpoint was not detected");

        SourceIndex appIndex = SourceIndex.FromSources(
            ("src/Apps/Clinic/Api/ClinicApi.cs", "namespace Clinic.Api; static class ClinicApi { static void Map(object group) => group.MapGet(\"/items\", () => 1); }"));
        Require(OpenApiEndpointCheck.Run(appIndex, validateAuthorizationMatrix: false).Count == 0, "app API endpoint was incorrectly included in module OpenAPI validation");

        SourceIndex excludedAssignedGroupIndex = SourceIndex.FromSources(
            ("src/Modules/Users/Infrastructure/Extensions/WebAuthnEndpointRouteBuilderExtensions.cs", """
                namespace Users.Infrastructure;
                static class WebAuthnEndpoints
                {
                    static void Map(object endpoints)
                    {
                        object webauthnGroup = endpoints.MapGroup("/api/webauthn");
                        webauthnGroup.ExcludeFromOpenApiWithJustification("infrastructure-owned");
                    }
                }
                """));
        Require(OpenApiEndpointCheck.Run(excludedAssignedGroupIndex, validateAuthorizationMatrix: false).Count == 0, "assigned excluded route group produced a violation");

        SourceIndex unexcludedAssignedGroupIndex = SourceIndex.FromSources(
            ("src/Modules/Users/Infrastructure/Extensions/WebAuthnEndpointRouteBuilderExtensions.cs", """
                namespace Users.Infrastructure;
                static class WebAuthnEndpoints
                {
                    static void Map(object endpoints)
                    {
                        object webauthnGroup = endpoints.MapGroup("/api/webauthn");
                    }
                }
                """));
        Require(OpenApiEndpointCheck.Run(unexcludedAssignedGroupIndex, validateAuthorizationMatrix: false).Any(violation => violation.Kind == "RawEndpointMapping"), "unexcluded assigned route group was not detected");
        AuthorizationMatrixCheckSelfTest();
        Console.WriteLine("OpenAPI endpoint Roslyn self-test passed");
    }

    private static void AuthorizationMatrixCheckSelfTest()
    {
        SourceIndex index = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object group) => group.MapModuleGet("/items", GetAsync, "FooGetItems");
                    static void GetAsync() { }
                }
                """),
            ("src/Modules/Users/Api/ProfileEndpoints.cs", """
                namespace Users.Api;
                static class ProfileEndpoints
                {
                    static void Map(object group) => group.MapModuleGet("/profile", GetAsync, "UsersGetProfile");
                    static void MapSearch(object group)
                    {
                        _ = group.MapModuleGet("/search", SearchAsync, "UsersSearchUsers").RequireAuthorization(FakePolicies.ReadUsers);
                    }
                    static void SearchAsync() { }
                    static bool GetAsync(UserSubjectAccessPolicy access, string userId) => access.AllowsSelf(userId);
                }
                """),
            ("src/Modules/Users/Module/AuthorizationPolicies.cs", """
                static class FakePolicies
                {
                    public const string ReadUsers = "Users.SecurityState.Read";
                    static void AddPolicies(AuthorizationOptions options) => options.AddPolicy("Users.SecurityState.Read", policy => policy.RequireRole("PlatformAdmin"));
                }
                static class IdentityDefinitions
                {
                    public const string PlatformAdmin = "PlatformAdmin";
                }
                """),
            ("src/Modules/Authentication/Api/PasswordRecoveryEndpoints.cs", """
                using Authentication.Contracts;
                namespace Authentication.Api;
                static class PasswordRecoveryEndpoints
                {
                    static void Map(object group)
                    {
                        group.MapModulePost("/first", BeginAsync, "Authentication_PasswordRecovery_Begin", profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordRecovery);
                        group.MapModulePost("/second", CompleteAsync, "Authentication_PasswordRecovery_Complete", profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: AuthenticationAnonymousEndpointJustifications.PasswordRecovery);
                    }
                    static void BeginAsync() { }
                    static void CompleteAsync() { }
                }
                """),
            ("src/Modules/Authentication/Contracts/AuthenticationAnonymousEndpointJustifications.cs", """
                namespace Authentication.Contracts;
                static class AuthenticationAnonymousEndpointJustifications
                {
                    public const string PasswordRecovery = "authentication.password-recovery";
                }
                """));
        string valid = """
            <!-- main-module-authorization-matrix:start -->
            | Operation ID | Module owner | Route owner | Classification | Authentication | Authority | Reauthentication | Public rationale | Regression test |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | FooGetItems | Foo | src/Modules/Foo/Api/FooApi.cs | protected | BrowserOrBearer | Foo application boundary | Not required | — | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | UsersGetProfile | Users | src/Modules/Users/Api/ProfileEndpoints.cs | protected | BrowserOrBearer | UserSubjectAccessPolicy applies the self-only rule | Not required | — | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | UsersSearchUsers | Users | src/Modules/Users/Api/ProfileEndpoints.cs | protected | BrowserOrBearer + Policy:FakePolicies.ReadUsers | PlatformAdmin policy | Not required | — | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | Authentication_PasswordRecovery_Begin | Authentication | src/Modules/Authentication/Api/PasswordRecoveryEndpoints.cs | anonymous | None | Public recovery request uses Authentication pre-authentication policy | Not required | authentication.password-recovery | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | Authentication_PasswordRecovery_Complete | Authentication | src/Modules/Authentication/Api/PasswordRecoveryEndpoints.cs | anonymous | None | Public recovery completion validates its single-use challenge and active password policy | Not required | authentication.password-recovery | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            <!-- main-module-authorization-matrix:end -->
            <!-- user-auth-role-object-matrix:start -->
            | Operation ID | Data category | Operation | PlatformAdmin | TenantOwner | TenantUser | Authorization owner | Regression test |
            | --- | --- | --- | --- | --- | --- | --- | --- |
            | UsersGetProfile | Account profile metadata | Read | Self only | Self only | Self only | UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | UsersSearchUsers | Account record | Read | Allow | Deny | Deny | FakePolicies.ReadUsers | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | Authentication_PasswordRecovery_Begin | Authentication challenge | Create | Pre-auth only | Pre-auth only | Pre-auth only | Anonymous endpoint mapping and Authentication password-recovery policy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            | Authentication_PasswordRecovery_Complete | Authentication credential | Change | Pre-auth only | Pre-auth only | Pre-auth only | Anonymous endpoint mapping and single-use password-recovery challenge | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            <!-- user-auth-role-object-matrix:end -->
            """;
        IReadOnlyList<ArchitectureViolation> validViolations = AuthorizationMatrixCheck.Run(index, valid);
        Require(validViolations.Count == 0, $"valid authorization matrix produced a violation: {string.Join("; ", validViolations.Select(violation => violation.Kind + ":" + violation.Expression + ":" + violation.Remediation))}");
        string widenedSelfAccess = valid.Replace("| Read | Self only | Self only | Self only |", "| Read | Allow | Self only | Self only |", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, widenedSelfAccess).Any(violation => violation.Kind == AuthorizationMatrixCheck.ExecutableDispositionMismatchKind),
            "self-only disposition widened to Allow without an executable authorization change");
        string widenedRoleAccess = valid.Replace("| Read | Allow | Deny | Deny | FakePolicies.ReadUsers |", "| Read | Allow | Allow | Deny | FakePolicies.ReadUsers |", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, widenedRoleAccess).Any(violation => violation.Kind == AuthorizationMatrixCheck.ExecutableDispositionMismatchKind),
            "platform-admin policy widened to TenantOwner without an executable authorization change");
        string arbitraryAuthority = valid.Replace("| Allow | Deny | Deny | FakePolicies.ReadUsers |", "| Allow | Deny | Deny | arbitrary prose |", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, arbitraryAuthority).Any(violation => violation.Kind == AuthorizationMatrixCheck.ExecutableAuthorityMismatchKind),
            "executable policy owner replaced by unbound prose");
        (string RelativePath, string Text)[] compoundPolicySources = index.Files.Select(file => (
            file.RelativePath,
            file.RelativePath == "src/Modules/Users/Module/AuthorizationPolicies.cs"
                ? file.Text.Replace("policy.RequireRole(\"PlatformAdmin\")", "policy.RequireRole(\"PlatformAdmin\").RequireRole(\"TenantOwner\")", StringComparison.Ordinal)
                : file.Text)).ToArray();
        SourceIndex compoundPolicyIndex = SourceIndex.FromSources(compoundPolicySources);
        Require(AuthorizationMatrixCheck.Run(compoundPolicyIndex, valid).Any(violation => violation.Kind == AuthorizationMatrixCheck.UserAuthRoleObjectDispositionInvalidKind),
            "compound authorization requirements were guessed instead of classified");
        string nonTestAnchor = valid.Replace(
            "UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |",
            "UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#MapSearch |", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, nonTestAnchor).Any(violation => violation.Kind == AuthorizationMatrixCheck.UserAuthRoleObjectTestReferenceMissingKind),
            "source method text without a declared test case was accepted as regression evidence");

        SourceIndex namedRationaleIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Contracts/FooContracts.cs", "namespace Foo.Contracts; public static class FooAnonymousJustifications { public const string Recovery = \"foo.password-recovery\"; }"),
            ("src/Modules/Foo/Api/FooApi.cs", """
                using Foo.Contracts;
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object group) => group.MapModulePost("/recovery", RecoverAsync, "FooPasswordRecovery", profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: FooAnonymousJustifications.Recovery);
                    static void RecoverAsync() { }
                }
                """));
        string namedRationaleMatrix = """
            <!-- main-module-authorization-matrix:start -->
            | Operation ID | Module owner | Route owner | Classification | Authentication | Authority | Reauthentication | Public rationale | Regression test |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | FooPasswordRecovery | Foo | src/Modules/Foo/Api/FooApi.cs | anonymous | None | Public recovery transport | Required | foo.password-recovery | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            <!-- main-module-authorization-matrix:end -->
            <!-- user-auth-role-object-matrix:start -->
            | Operation ID | Data category | Operation | PlatformAdmin | TenantOwner | TenantUser | Authorization owner | Regression test |
            | --- | --- | --- | --- | --- | --- | --- | --- |
            <!-- user-auth-role-object-matrix:end -->
            """;
        Require(AuthorizationMatrixCheck.Run(namedRationaleIndex, namedRationaleMatrix).Count == 0,
            "source-owned anonymous rationale constant did not match its matrix row");
        string mismatchedNamedRationale = namedRationaleMatrix.Replace("foo.password-recovery", "foo.other", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(namedRationaleIndex, mismatchedNamedRationale)
                .Any(violation => violation.Kind == "AnonymousRationaleMismatch"),
            "source-owned anonymous rationale mismatch was not detected");

        string duplicate = valid.Replace(
            "<!-- main-module-authorization-matrix:end -->",
            "| FooGetItems | Foo | src/Modules/Foo/Api/FooApi.cs | protected | BrowserOrBearer | Foo application boundary | Not required | — | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |\n<!-- main-module-authorization-matrix:end -->",
            StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, duplicate).Any(violation => violation.Kind == "DuplicateAuthorizationMatrixOperation"),
            "duplicate authorization matrix operation was not detected");

        string stale = valid.Replace("FooGetItems", "FooStaleOperation", StringComparison.Ordinal);
        IReadOnlyList<ArchitectureViolation> staleViolations = AuthorizationMatrixCheck.Run(index, stale);
        Require(staleViolations.Any(violation => violation.Kind == "MissingAuthorizationMatrixOperation"), "missing authorization matrix operation was not detected");
        Require(staleViolations.Any(violation => violation.Kind == "StaleAuthorizationMatrixOperation"), "stale authorization matrix operation was not detected");

        string missingDisposition = valid.Replace("| UsersGetProfile | Account profile metadata | Read | Self only | Self only | Self only | UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |", string.Empty, StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, missingDisposition).Any(violation => violation.Kind == "MissingUserAuthRoleObjectOperation"),
            "missing Users or Authentication role/object disposition was not detected");
        string duplicateDisposition = valid.Replace(
            "<!-- user-auth-role-object-matrix:end -->",
            "| UsersGetProfile | Account profile metadata | Read | Self only | Self only | Self only | UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |\n<!-- user-auth-role-object-matrix:end -->",
            StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, duplicateDisposition).Any(violation => violation.Kind == "DuplicateUserAuthRoleObjectOperation"),
            "duplicate Users or Authentication role/object disposition was not detected");
        string staleDisposition = valid.Replace(
            "<!-- user-auth-role-object-matrix:end -->",
            "| UsersStaleProfile | Account profile metadata | Read | Self only | Self only | Self only | UserSubjectAccessPolicy | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |\n<!-- user-auth-role-object-matrix:end -->",
            StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, staleDisposition).Any(violation => violation.Kind == "StaleUserAuthRoleObjectOperation"),
            "stale Users or Authentication role/object disposition was not detected");
        string invalidDisposition = valid.Replace("| Self only | Self only | Self only |", "| Unknown | Self only | Self only |", StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, invalidDisposition).Any(violation => violation.Kind == AuthorizationMatrixCheck.UserAuthRoleObjectDispositionInvalidKind),
            "invalid subject disposition was not detected");
        string missingTest = valid.Replace(
            "tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |\n<!-- user-auth-role-object-matrix:end -->",
            "missing-test.cs#Absent |\n<!-- user-auth-role-object-matrix:end -->",
            StringComparison.Ordinal);
        Require(AuthorizationMatrixCheck.Run(index, missingTest).Any(violation => violation.Kind == "UserAuthRoleObjectTestMissing"),
            "missing Users or Authentication regression test was not detected");
    }

    private static void SecurityBaselineSelfTest()
    {
        SourceIndex validIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Contracts/FooContracts.cs", "namespace Foo.Contracts; public sealed record ResponseDto;"),
            ("src/Modules/Foo/Api/FooApi.cs", """
                using Foo.Contracts;
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapProtectedModuleGroup("/api/foo");
                        group.MapModuleGet("/items", Get, "FooGetItems", typeof(ResponseDto));
                    }
                    static void Get() { }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(validIndex).Count == 0, "valid security endpoint produced a violation");

        SourceIndex defaultHeadIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapProtectedModuleGroup("/api/foo");
                        group.MapModuleHead("/download", Head, "FooHeadDownload");
                    }
                    static void Head() { }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(defaultHeadIndex).Count == 0, "default HEAD profile produced a violation");

        SourceIndex anonymousDownloadIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapAnonymousModuleGroup("/api/foo/public", "Public images.");
                        group.MapModuleGet("/image", Get, "FooGetImage", profile: ModuleEndpointProfile.AnonymousBinaryDownload, anonymousJustification: "Public images.", contentType: "application/octet-stream");
                    }
                    static void Get() { }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(anonymousDownloadIndex).Count == 0, "anonymous binary download produced a violation");

        SourceIndex anonymousProtectedDownloadIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapAnonymousModuleGroup("/api/foo/public", "Public images.");
                        group.MapModuleGet("/image", Get, "FooGetImage", profile: ModuleEndpointProfile.BinaryDownload, contentType: "application/octet-stream");
                    }
                    static void Get() { }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(anonymousProtectedDownloadIndex).Any(violation => violation.Kind == "AuthenticationProfileConflict"), "protected download in an anonymous group was not rejected");

        SourceIndex rawValidIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapProtectedModuleGroup("/api/foo");
                        group.MapPost("/items", () => 1)
                            .WithMetadata(new ModuleEndpointOpenApiConvention("FooCreateItem", 201, Profile: ModuleEndpointProfile.ProtectedJson))
                            .AddEndpointFilter<SecurityAuditEndpointFilter>();
                    }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(rawValidIndex).Count == 0, "typed raw endpoint produced a violation");

        SourceIndex rawMissingAuditIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapProtectedModuleGroup("/api/foo");
                        group.MapPost("/items", () => 1)
                            .WithMetadata(new ModuleEndpointOpenApiConvention("FooCreateItem", 201, Profile: ModuleEndpointProfile.ProtectedJson));
                    }
                }
                """));
        Require(SecurityBaselineCheck.RunEndpoints(rawMissingAuditIndex).Any(violation => violation.Kind == "AuditDispositionMissing"), "raw audit filter omission was not rejected");

        SourceIndex invalidIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", """
                namespace Foo.Api;
                static class FooApi
                {
                    static void Map(object endpoints)
                    {
                        var group = endpoints.MapGroup(GetPrefix());
                        group.MapGet(GetPattern(), () => 1);
                    }
                    static string GetPrefix() => "/api/foo";
                    static string GetPattern() => "/items";
                }
                """));
        IReadOnlyList<ArchitectureViolation> invalid = SecurityBaselineCheck.RunEndpoints(invalidIndex);
        Require(invalid.Any(violation => violation.Kind == "RouteUnresolved"), "dynamic route was not rejected");
        Require(invalid.Any(violation => violation.Kind == "OperationIdMissing"), "raw endpoint operation ID was not rejected");
        Require(invalid.Any(violation => violation.Kind == "AuthenticationUnresolved"), "unclassified authentication was not rejected");
        Require(invalid.Any(violation => violation.Kind == "UserDataOperationDispositionMissing"), "incomplete user-data operation inventory was not rejected");
        SourceSecuritySurfaceSelfTest();
        Console.WriteLine("Security baseline Roslyn self-test passed");
    }

    private static void SourceSecuritySurfaceSelfTest()
    {
        SourceIndex safeIndex = SourceIndex.FromSources(
            ("src/Hosts/Main/Program.cs", """
                app.UseCors(policy => policy.WithOrigins("https://example.test").WithMethods("GET", "POST").AllowCredentials());
                response.Cookies.Append("session", value, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
                """));
        Require(SecurityBaselineCheck.RunSourceSecuritySurface(safeIndex).Count == 0, "safe source security surface produced a violation");

        SourceIndex unsafeIndex = SourceIndex.FromSources(
            ("src/Hosts/Main/Program.cs", """
                app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod());
                app.MapMethods(GetPath(), GetMethods(), () => 1);
                var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
                var options = new { UseSsl = false };
                response.Cookies.Append("session", value, cookieOptions);
                static string GetPath() => "/unsafe";
                static string GetMethods() => "GET";
                """));
        IReadOnlyList<ArchitectureViolation> violations = SecurityBaselineCheck.RunSourceSecuritySurface(unsafeIndex);
        Require(violations.Any(violation => violation.Kind == "UnsafeCorsPolicy"), "unsafe CORS was not rejected");
        Require(violations.Any(violation => violation.Kind == "RouteMethodUnresolved"), "dynamic method set was not rejected");
        Require(violations.Any(violation => violation.Kind == "CertificateValidationOverride"), "certificate callback was not rejected");
        Require(violations.Any(violation => violation.Kind == "OutboundTlsDisabled"), "disabled outbound TLS was not rejected");
        Require(violations.Any(violation => violation.Kind == "CookieDispositionMissing"), "insecure cookie disposition was not rejected");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
