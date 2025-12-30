using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class AdminContextTests
{
    [Fact]
    public void AdminContext_IsAdmin_ReturnsTrue_ForAdminRole()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(c => c.UserId).Returns("admin");
        requestContext.SetupGet(c => c.OwnerId).Returns("admin");

        AdminContext sut = new(requestContext.Object);

        sut.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void AdminContext_IsAdmin_ReturnsFalse_ForNonAdmin()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(c => c.UserId).Returns("user");
        requestContext.SetupGet(c => c.OwnerId).Returns("owner");

        AdminContext sut = new(requestContext.Object);

        sut.IsAdmin.Should().BeFalse();
    }
}
