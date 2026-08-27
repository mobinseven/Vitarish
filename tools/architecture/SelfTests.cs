namespace Vitarish.Tools.Architecture;

internal static class SelfTests
{
    public static void Run()
    {
        InternalModuleClientCheckSelfTest();
        OpenApiEndpointCheckSelfTest();
        SecurityBaselineSelfTest();
        Console.WriteLine("Architecture Roslyn self-tests passed");
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
                """));
        string valid = """
            <!-- main-module-authorization-matrix:start -->
            | Operation ID | Module owner | Route owner | Classification | Authentication | Authority | Reauthentication | Public rationale | Regression test |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | FooGetItems | Foo | src/Modules/Foo/Api/FooApi.cs | protected | BrowserOrBearer | Foo application boundary | Not required | — | tools/architecture/SelfTests.cs#AuthorizationMatrixCheckSelfTest |
            <!-- main-module-authorization-matrix:end -->
            """;
        Require(AuthorizationMatrixCheck.Run(index, valid).Count == 0, "valid authorization matrix produced a violation");

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
