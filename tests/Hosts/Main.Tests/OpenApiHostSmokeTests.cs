using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Hosts.Main.Tests;

public sealed class OpenApiHostSmokeTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    [Fact]
    public void MainHost_ExposesOpenApiOnlyFromDevelopmentRoute()
    {
        string program = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Hosts/Main/Program.cs"));

        program.Should().Contain("builder.Services.AddOpenApi()", "the host composition root owns OpenAPI document registration");
        program.Should().Contain("app.Environment.IsDevelopment()", "ADR-0005 limits document exposure to the configured development condition");
        program.Should().Contain("app.MapOpenApi(\"/openapi/{documentName}.json\")", "the configured OpenAPI document route must remain stable for smoke checks");
    }

    [Fact]
    public void RepresentativeModuleEndpoints_DeclareStableOpenApiMetadata()
    {
        string usersApi = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Modules/Users/Api/UsersApi.cs"));
        string filesApi = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Modules/Files/Api/FilesApiEndpointMapping.cs"));
        string socialWebhooks = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Modules/Social/Module/Endpoints/SocialWebhookEndpointRouteBuilderExtensions.cs"));

        usersApi.Should().Contain("MapProtectedModuleGroup(UsersApiRoutes.Base)");
        usersApi.Should().Contain("UsersSearchUsers", "operation IDs are the stable client-facing OpenAPI identifiers");
        usersApi.Should().Contain("typeof(UserSummaryResponse[])", "response schemas must resolve to Users Contracts-owned shapes");

        filesApi.Should().Contain("FilesDownloadFile");
        filesApi.Should().Contain("ModuleEndpointProfile.BinaryDownload");
        filesApi.Should().Contain("contentType: ModuleEndpointOpenApiConventions.BinaryStream", "binary OpenAPI profiles require explicit content metadata");
        filesApi.Should().Contain("typeof(FileUploadResponse)", "multipart upload responses must be contract-owned schemas");

        socialWebhooks.Should().Contain("MapAnonymousModuleGroup");
        socialWebhooks.Should().Contain("anonymousJustification:", "anonymous endpoints require ADR-0005 justification metadata");
    }

    [Fact]
    public void ExcludedAccountAndWebAuthnEndpoints_UseTypedJustificationHelper()
    {
        string accounts = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Interfaces/Interfaces.Accounts/Extensions/IdentityComponentsEndpointRouteBuilderExtensions.cs"));
        string webAuthn = File.ReadAllText(Path.Combine(RepositoryRoot, "src/Modules/Users/Infrastructure/Extensions/WebAuthnEndpointRouteBuilderExtensions.cs"));

        accounts.Should().Contain("ExcludeFromOpenApiWithJustification(");
        webAuthn.Should().Contain("ExcludeFromOpenApiWithJustification(");
        Regex rawMetadata = new("OpenApiExclusionJustification:");
        rawMetadata.IsMatch(accounts).Should().BeFalse("raw string metadata is not an accepted ADR-0005 exclusion convention");
        rawMetadata.IsMatch(webAuthn).Should().BeFalse("raw string metadata is not an accepted ADR-0005 exclusion convention");
    }
}
