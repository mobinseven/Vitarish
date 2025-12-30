using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class PersonalAccessTokenServiceTests
{
    [Fact]
    public async Task PersonalAccessTokenService_CreateToken_ReturnsToken()
    {
        Mock<IPersonalAccessTokenGenerator> generator = new();
        generator.Setup(g => g.CreateTokenAsync("user-1", null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-value");

        PersonalAccessTokenService sut = new(generator.Object);

        string token = await sut.CreateTokenAsync("user-1");

        token.Should().Be("token-value");
    }

    [Fact]
    public async Task PersonalAccessTokenService_RevokeToken_RemovesToken()
    {
        Mock<IPersonalAccessTokenGenerator> generator = new();
        generator.Setup(g => g.CreateTokenAsync("user-2", null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-a");

        PersonalAccessTokenService sut = new(generator.Object);

        string token = await sut.CreateTokenAsync("user-2");
        await sut.RevokeTokenAsync("user-2", token);

        IReadOnlyCollection<string> tokens = await sut.ListTokensAsync("user-2");

        tokens.Should().NotContain(token);
    }

    [Fact]
    public async Task PersonalAccessTokenService_ListTokens_ReturnsUserTokens()
    {
        Mock<IPersonalAccessTokenGenerator> generator = new();
        generator.Setup(g => g.CreateTokenAsync("user-3", null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-b");
        generator.Setup(g => g.CreateTokenAsync("user-4", null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-c");

        PersonalAccessTokenService sut = new(generator.Object);

        _ = await sut.CreateTokenAsync("user-3");
        _ = await sut.CreateTokenAsync("user-4");

        IReadOnlyCollection<string> tokens = await sut.ListTokensAsync("user-3");

        tokens.Should().ContainSingle(token => token == "token-b");
    }
}
