using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class UpdateUserClaimsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRemoveFails_ReturnsFailure()
    {
        User user = new() { Id = "user-1" };
        Claim existing = new("scope", "read");
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([existing]);
        userManager.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "remove failed" }));

        UpdateUserClaimsCommandHandler handler = new(userManager.Object);

        IdentityResult result = await handler.Handle(new UpdateUserClaimsCommand(user, [new Claim("scope", "write")]), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        userManager.Verify(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenClaimsChange_RemovesAndAddsExpectedClaims()
    {
        User user = new() { Id = "user-1" };
        Claim oldClaim = new("scope", "read");
        Claim keepClaim = new("role", "admin");
        Claim newClaim = new("scope", "write");

        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([oldClaim, keepClaim]);
        userManager.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        UpdateUserClaimsCommandHandler handler = new(userManager.Object);

        IdentityResult result = await handler.Handle(new UpdateUserClaimsCommand(user, [keepClaim, newClaim]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        userManager.Verify(m => m.RemoveClaimsAsync(user,
            It.Is<IEnumerable<Claim>>(claims => claims.Single().Type == "scope" && claims.Single().Value == "read")), Times.Once);
        userManager.Verify(m => m.AddClaimsAsync(user,
            It.Is<IEnumerable<Claim>>(claims => claims.Single().Type == "scope" && claims.Single().Value == "write")), Times.Once);
    }
}
