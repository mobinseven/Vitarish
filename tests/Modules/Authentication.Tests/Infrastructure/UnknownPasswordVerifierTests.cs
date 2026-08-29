using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class UnknownPasswordVerifierTests
{
    [Fact]
    public void Uses_the_configured_hasher_for_dummy_work_and_verification()
    {
        Mock<IPasswordHasher<User>> hasher = new();
        hasher.Setup(item => item.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("dummy-hash");
        hasher.Setup(item => item.VerifyHashedPassword(It.IsAny<User>(), "dummy-hash", "submitted-password"))
            .Returns(PasswordVerificationResult.Failed);

        UnknownPasswordVerifier verifier = new(hasher.Object);
        verifier.Verify("submitted-password");

        hasher.Verify(item => item.HashPassword(
            It.Is<User>(user => user.Id == "authentication-unknown-login-timing-only"),
            It.IsAny<string>()), Times.Once);
        hasher.Verify(item => item.VerifyHashedPassword(
            It.Is<User>(user => user.Id == "authentication-unknown-login-timing-only"),
            "dummy-hash",
            "submitted-password"), Times.Once);
    }
}
