using FluentAssertions;
using Moq;
using Vitarish.Medical.Infrastructure.Handlers.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Medical.Tests.Infrastructure.Handlers.Prescriptions;

public sealed class CreateDraftTenantScopeTests
{
    [Fact]
    public void ResolveTenantId_uses_the_authenticated_request_tenant()
    {
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.TenantId).Returns(" tenant-1 ");

        CreateDraftHandler.ResolveTenantId(context.Object).Should().Be("tenant-1");
    }

    [Fact]
    public void ResolveTenantId_rejects_a_missing_or_blank_request_tenant()
    {
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.TenantId).Returns(" ");

        CreateDraftHandler.ResolveTenantId(context.Object).Should().BeNull();
    }
}
