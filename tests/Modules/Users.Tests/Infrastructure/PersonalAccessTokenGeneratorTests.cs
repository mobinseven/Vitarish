using Duende.IdentityServer.Services;
using System.Security.Claims;
using System.Text.Json;
using Users.Tests.TestHelpers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Service;

namespace Users.Tests.Infrastructure;

public class PersonalAccessTokenGeneratorTests
{
    [Fact]
    public async Task CreateTokenAsync_ThrowsWhenDefaultLifetimeInvalid()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        Mock<ITokenService> tokenService = new();
        Mock<IIssuerNameService> issuerNameService = new();
        Mock<IRequestContext> requestContext = new();
        IOptions<PersonalAccessTokenOptions> options = Options.Create(new PersonalAccessTokenOptions { DefaultLifetimeDays = 0 });

        PersonalAccessTokenGenerator generator = new(userManager.Object, tokenService.Object, issuerNameService.Object, requestContext.Object, options);

        Func<Task> act = () => generator.CreateTokenAsync("user-1", null, null, false, CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one day*");
    }

    [Fact]
    public async Task CreateTokenAsync_ThrowsWhenLifetimeOverrideInvalid()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        Mock<ITokenService> tokenService = new();
        Mock<IIssuerNameService> issuerNameService = new();
        Mock<IRequestContext> requestContext = new();
        IOptions<PersonalAccessTokenOptions> options = Options.Create(new PersonalAccessTokenOptions { DefaultLifetimeDays = 10 });

        PersonalAccessTokenGenerator generator = new(userManager.Object, tokenService.Object, issuerNameService.Object, requestContext.Object, options);

        Func<Task> act = () => generator.CreateTokenAsync("user-1", 0, null, false, CancellationToken.None);

        _ = await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateTokenAsync_ThrowsWhenUserMissing()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync((User?)null);
        Mock<ITokenService> tokenService = new();
        Mock<IIssuerNameService> issuerNameService = new();
        Mock<IRequestContext> requestContext = new();
        IOptions<PersonalAccessTokenOptions> options = Options.Create(new PersonalAccessTokenOptions());

        PersonalAccessTokenGenerator generator = new(userManager.Object, tokenService.Object, issuerNameService.Object, requestContext.Object, options);

        Func<Task> act = () => generator.CreateTokenAsync("user-1", null, null, false, CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task CreateTokenAsync_ThrowsWhenInvalidScopesRequested()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _ = userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim(IdentityDefinitions.AccessClaim, "allowed")]);
        _ = userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        Mock<ITokenService> tokenService = new();
        Mock<IIssuerNameService> issuerNameService = new();
        Mock<IRequestContext> requestContext = new();
        IOptions<PersonalAccessTokenOptions> options = Options.Create(new PersonalAccessTokenOptions());

        PersonalAccessTokenGenerator generator = new(userManager.Object, tokenService.Object, issuerNameService.Object, requestContext.Object, options);

        Func<Task> act = () => generator.CreateTokenAsync("user-1", null, new[] { "invalid" }, false, CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not permitted*");
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsCachedTokenWhenReusable()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _ = userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim(IdentityDefinitions.AccessClaim, "scope1")]);
        _ = userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        string cachedPayload = JsonSerializer.Serialize(new
        {
            Token = "cached-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Scopes = new[] { "scope1", IdentityDefinitions.AccessScope },
            IsReferenceToken = false
        });
        _ = userManager.Setup(m => m.GetAuthenticationTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(cachedPayload);

        Mock<ITokenService> tokenService = new();
        Mock<IIssuerNameService> issuerNameService = new();
        Mock<IRequestContext> requestContext = new();
        IOptions<PersonalAccessTokenOptions> options = Options.Create(new PersonalAccessTokenOptions());

        PersonalAccessTokenGenerator generator = new(userManager.Object, tokenService.Object, issuerNameService.Object, requestContext.Object, options);

        string token = await generator.CreateTokenAsync("user-1", null, new[] { "scope1" }, false, CancellationToken.None);

        _ = token.Should().Be("cached-token");
        tokenService.Verify(t => t.CreateSecurityTokenAsync(It.IsAny<Duende.IdentityServer.Models.Token>()), Times.Never);
    }
}
