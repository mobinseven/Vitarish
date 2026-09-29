using Moq;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Users.Tests.Security;

public sealed class UserSubjectAccessPolicyTests
{
    [Fact]
    public void Allows_only_the_authenticated_subject_to_access_self_service_resources()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.Setup(context => context.UserId).Returns("subject-a");
        UserSubjectAccessPolicy policy = new(requestContext.Object);

        Assert.True(policy.AllowsSelf("subject-a"));
        Assert.False(policy.AllowsSelf("subject-b"));
        Assert.False(policy.AllowsSelf(string.Empty));
    }

    [Fact]
    public void Denies_self_service_resource_access_when_the_request_has_no_subject()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.Setup(context => context.UserId).Returns((string?)null);
        UserSubjectAccessPolicy policy = new(requestContext.Object);

        Assert.False(policy.AllowsSelf("subject-a"));
    }
}
