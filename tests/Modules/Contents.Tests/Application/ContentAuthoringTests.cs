using System.Security.Claims;
using System.Text.Json;
using Moq;
using Vitarish.Contents.Application;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Tests.Application;

public sealed class ContentAuthoringTests
{
    private const string TenantId = "tenant-under-test";
    private const string UserId = "user-under-test";

    [Fact]
    public async Task SaveDraft_returns_concurrency_for_stale_expected_version()
    {
        Guid contentId = Guid.NewGuid();
        Guid siteId = Guid.NewGuid();
        Guid typeId = Guid.NewGuid();
        Mock<IContentAuthoringStore> store = new();
        store.Setup(value => value.GetEntryByIdAsync(TenantId, contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentItem
            {
                Id = contentId,
                SiteId = siteId,
                ContentTypeId = typeId,
                SchemaVersion = 1,
                Version = 2
            });
        store.Setup(value => value.GetSiteByIdAsync(TenantId, siteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Site { Id = siteId, Locales = ["en"] });
        store.Setup(value => value.GetContentTypeByIdAsync(TenantId, typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentType { Id = typeId, CurrentSchemaVersion = 1 });
        store.Setup(value => value.GetContentTypeSchemaAsync(TenantId, typeId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentTypeSchema { ContentTypeId = typeId, Version = 1 });
        ContentAuthoring authoring = new(store.Object, RequestContext(isTenantOwner: true), Mock.Of<IEntityTimeService>());
        using JsonDocument fields = JsonDocument.Parse("{}");

        EntityResult<ContentAuthoringOutcome> result = await authoring.SaveDraftAsync(
            contentId,
            expectedVersion: 1,
            slug: "slug-under-test",
            path: "path-under-test",
            title: "title-under-test",
            excerpt: null,
            fields.RootElement,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(EntityErrorCode.Concurrency);
        store.Verify(value => value.SaveRevisionAsync(
            It.IsAny<string>(),
            It.IsAny<ContentItem>(),
            It.IsAny<ContentRevision>(),
            It.IsAny<int?>(),
            It.IsAny<ContentPublicationOperation?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_returns_unauthorized_for_non_owner()
    {
        Mock<IContentAuthoringStore> store = new();
        ContentAuthoring authoring = new(store.Object, RequestContext(isTenantOwner: false), Mock.Of<IEntityTimeService>());
        using JsonDocument fields = JsonDocument.Parse("{}");

        EntityResult<ContentAuthoringOutcome> result = await authoring.SaveDraftAsync(
            Guid.NewGuid(),
            expectedVersion: 1,
            slug: "slug-under-test",
            path: "path-under-test",
            title: "title-under-test",
            excerpt: null,
            fields.RootElement,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(EntityErrorCode.Unauthorized);
        store.Invocations.Should().BeEmpty();
    }

    private static IRequestContext RequestContext(bool isTenantOwner)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.TenantId).Returns(TenantId);
        context.SetupGet(value => value.UserId).Returns(UserId);
        ClaimsPrincipal principal = isTenantOwner
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, $"{TenantId}:{IdentityDefinitions.TenantOwner}")],
                "test"))
            : new ClaimsPrincipal(new ClaimsIdentity());
        context.SetupGet(value => value.User).Returns(principal);
        return context.Object;
    }
}
