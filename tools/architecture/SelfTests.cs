namespace Vitarish.Tools.Architecture;

internal static class SelfTests
{
    public static void Run()
    {
        InternalModuleClientCheckSelfTest();
        OpenApiEndpointCheckSelfTest();
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
        Require(OpenApiEndpointCheck.Run(validIndex).Count == 0, "valid OpenAPI endpoint produced a violation");

        SourceIndex invalidIndex = SourceIndex.FromSources(
            ("src/Modules/Foo/Api/FooApi.cs", "namespace Foo.Api; static class FooApi { static void Map(object group) => group.MapGet(\"/items\", () => 1); }"));
        Require(OpenApiEndpointCheck.Run(invalidIndex).Any(violation => violation.Kind == "RawEndpointMapping"), "raw endpoint was not detected");

        SourceIndex appIndex = SourceIndex.FromSources(
            ("src/Apps/Clinic/Api/ClinicApi.cs", "namespace Clinic.Api; static class ClinicApi { static void Map(object group) => group.MapGet(\"/items\", () => 1); }"));
        Require(OpenApiEndpointCheck.Run(appIndex).Count == 0, "app API endpoint was incorrectly included in module OpenAPI validation");

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
        Require(OpenApiEndpointCheck.Run(excludedAssignedGroupIndex).Count == 0, "assigned excluded route group produced a violation");

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
        Require(OpenApiEndpointCheck.Run(unexcludedAssignedGroupIndex).Any(violation => violation.Kind == "RawEndpointMapping"), "unexcluded assigned route group was not detected");
        Console.WriteLine("OpenAPI endpoint Roslyn self-test passed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
