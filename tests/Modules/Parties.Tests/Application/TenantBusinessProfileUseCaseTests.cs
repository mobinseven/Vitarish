using FluentAssertions;
using Moq;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Tests.Application;

public sealed class TenantBusinessProfileUseCaseTests
{
    private const string OfferedCategory = "dermatology";
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly SelfServiceTenantProfile Profile = new("clinic-app", [OfferedCategory]);

    [Fact]
    public async Task Saves_trimmed_fields_for_the_request_tenant_after_checking_its_photos()
    {
        Guid photo = Guid.NewGuid();
        TenantBusinessDetails details = WithPhotos(photo);
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.SaveAsync(TenantId, "Skin care", OfferedCategory, details, FixedNow.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<ITenantImageFiles> images = new();
        images.Setup(item => item.AreOwnImagesAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { photo })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        TenantBusinessProfileUpdateResult result = await UpdateHandler(store.Object, images.Object)
            .Handle(new UpdateTenantBusinessProfileCommand(" Skin care ", $" {OfferedCategory} ", details), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.Updated);
        store.VerifyAll();
    }

    [Fact]
    public async Task Refuses_photos_that_are_not_the_tenants_own_images_without_saving()
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);
        Mock<ITenantImageFiles> images = new();
        images.Setup(item => item.AreOwnImagesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        TenantBusinessProfileUpdateResult result = await UpdateHandler(store.Object, images.Object)
            .Handle(new UpdateTenantBusinessProfileCommand(null, null, WithPhotos(Guid.NewGuid())), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.Invalid);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("astrology", true)]
    [InlineData(OfferedCategory, false)]
    public async Task Refuses_a_category_the_app_does_not_offer(string category, bool registerProfile)
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);

        TenantBusinessProfileUpdateResult result = await UpdateHandler(store.Object, Mock.Of<ITenantImageFiles>(), registerProfile: registerProfile)
            .Handle(new UpdateTenantBusinessProfileCommand(null, category, null), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.Invalid);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refuses_a_description_over_the_limit()
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);

        TenantBusinessProfileUpdateResult result = await UpdateHandler(store.Object, Mock.Of<ITenantImageFiles>())
            .Handle(new UpdateTenantBusinessProfileCommand(new string('x', CreateOwnTenantCommandHandler.DescriptionMaximumLength + 1), null, null), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.Invalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Answers_not_found_without_a_tenant_in_the_request(string? tenantId)
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);

        TenantBusinessProfileUpdateResult update = await new UpdateTenantBusinessProfileCommandHandler(
                store.Object, Mock.Of<ITenantImageFiles>(), RequestContext(tenantId), new FixedTimeProvider(FixedNow), Profile)
            .Handle(new UpdateTenantBusinessProfileCommand(null, null, null), CancellationToken.None);
        TenantBusinessProfileUpdateResult dismiss = await new DismissTenantLogoPromptCommandHandler(
                store.Object, RequestContext(tenantId), new FixedTimeProvider(FixedNow))
            .Handle(new DismissTenantLogoPromptCommand(), CancellationToken.None);
        TenantBusinessProfileView? view = await new GetTenantBusinessProfileQueryHandler(store.Object, RequestContext(tenantId))
            .Handle(new GetTenantBusinessProfileQuery(), CancellationToken.None);

        update.Status.Should().Be(TenantBusinessProfileUpdateStatus.NotFound);
        dismiss.Status.Should().Be(TenantBusinessProfileUpdateStatus.NotFound);
        view.Should().BeNull();
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Answers_not_found_when_the_store_has_no_such_tenant()
    {
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.SaveAsync(TenantId, null, null, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        TenantBusinessProfileUpdateResult result = await UpdateHandler(store.Object, Mock.Of<ITenantImageFiles>())
            .Handle(new UpdateTenantBusinessProfileCommand(null, null, null), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.NotFound);
    }

    [Fact]
    public async Task Sets_the_logo_to_the_images_content_address()
    {
        Guid fileId = Guid.NewGuid();
        Mock<ITenantImageFiles> images = new();
        images.Setup(item => item.AreOwnImagesAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { fileId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        images.Setup(item => item.ContentUrl(fileId)).Returns("/files/logo");
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.SetLogoUrlAsync(TenantId, "/files/logo", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        TenantBusinessProfileUpdateResult result = await new SetTenantLogoCommandHandler(store.Object, images.Object, RequestContext(TenantId.ToString()))
            .Handle(new SetTenantLogoCommand(fileId), CancellationToken.None);

        result.Status.Should().Be(TenantBusinessProfileUpdateStatus.Updated);
        store.VerifyAll();
    }

    [Fact]
    public async Task Refuses_a_logo_that_is_not_the_tenants_own_image()
    {
        Mock<ITenantImageFiles> images = new();
        images.Setup(item => item.AreOwnImagesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);
        SetTenantLogoCommandHandler handler = new(store.Object, images.Object, RequestContext(TenantId.ToString()));

        TenantBusinessProfileUpdateResult foreign = await handler.Handle(new SetTenantLogoCommand(Guid.NewGuid()), CancellationToken.None);
        TenantBusinessProfileUpdateResult empty = await handler.Handle(new SetTenantLogoCommand(Guid.Empty), CancellationToken.None);

        foreign.Status.Should().Be(TenantBusinessProfileUpdateStatus.Invalid);
        empty.Status.Should().Be(TenantBusinessProfileUpdateStatus.Invalid);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Opens_the_request_tenants_logo_at_its_stored_address()
    {
        TenantImageContent content = new(new MemoryStream([1]), "image/png", 1);
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.GetAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantBusinessProfileView("Clinic", null, "/files/logo", null, null, false));
        Mock<ITenantImageFiles> images = new();
        images.Setup(item => item.OpenAsync("/files/logo", It.IsAny<CancellationToken>())).ReturnsAsync(content);

        TenantImageContent? logo = await new GetPublicTenantLogoQueryHandler(store.Object, images.Object, RequestContext(TenantId.ToString()))
            .Handle(new GetPublicTenantLogoQuery(), CancellationToken.None);

        logo.Should().BeSameAs(content);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("not-a-guid", true)]
    [InlineData("tenant", false)]
    public async Task Opens_no_logo_without_a_request_tenant_or_one_it_has_set(string? tenantId, bool tenantHasLogo)
    {
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.GetAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantBusinessProfileView("Clinic", null, tenantHasLogo ? "/files/logo" : null, null, null, false));
        Mock<ITenantImageFiles> images = new(MockBehavior.Strict);

        TenantImageContent? logo = await new GetPublicTenantLogoQueryHandler(
                store.Object, images.Object, RequestContext(tenantId == "tenant" ? TenantId.ToString() : tenantId))
            .Handle(new GetPublicTenantLogoQuery(), CancellationToken.None);

        logo.Should().BeNull();
        images.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, TenantBusinessProfileUpdateStatus.Updated)]
    [InlineData(false, TenantBusinessProfileUpdateStatus.NotFound)]
    public async Task Dismisses_the_logo_prompt_for_the_request_tenant_at_the_current_time(bool saved, TenantBusinessProfileUpdateStatus expected)
    {
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.DismissLogoPromptAsync(TenantId, FixedNow.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        TenantBusinessProfileUpdateResult result = await new DismissTenantLogoPromptCommandHandler(
                store.Object, RequestContext(TenantId.ToString()), new FixedTimeProvider(FixedNow))
            .Handle(new DismissTenantLogoPromptCommand(), CancellationToken.None);

        result.Status.Should().Be(expected);
        store.VerifyAll();
    }

    private static UpdateTenantBusinessProfileCommandHandler UpdateHandler(
        ITenantBusinessProfileStore store,
        ITenantImageFiles images,
        bool registerProfile = true) =>
        new(store, images, RequestContext(TenantId.ToString()), new FixedTimeProvider(FixedNow), registerProfile ? Profile : null);

    private static IRequestContext RequestContext(string? tenantId)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns(tenantId);
        return requestContext.Object;
    }

    private static TenantBusinessDetails WithPhotos(params Guid[] photos) =>
        TenantBusinessDetails.TryCreate(null, ["021 1234 5678"], null, null, null, photos, out _)!;

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
