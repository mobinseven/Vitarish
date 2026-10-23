using FluentAssertions;
using Moq;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Tests.Application;

public sealed class OnboardingSetupDraftTests
{
    private static readonly SelfServiceTenantProfile Profile = new("clinic-app", ["dentistry", "dermatology"]);

    [Fact]
    public async Task Saves_the_signed_in_persons_draft_with_an_offered_category()
    {
        Mock<IOnboardingSetupDraftStore> drafts = new();
        drafts.Setup(store => store.SaveAsync("user-1", It.IsAny<OnboardingSetupDraft>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        SaveOnboardingSetupDraftOutcome outcome = await new SaveOnboardingSetupDraftCommandHandler(drafts.Object, Context("user-1"), Profile)
            .Handle(new("dentistry", ["phone", "no-shows", "no-shows"]), default);

        outcome.Should().Be(SaveOnboardingSetupDraftOutcome.Saved);
        drafts.Verify(store => store.SaveAsync("user-1",
            It.Is<OnboardingSetupDraft>(draft => draft.Category == "dentistry" && draft.Answers.SequenceEqual(new[] { "phone", "no-shows" })),
            It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData("psychology", "phone")]
    [InlineData("dentistry", "Not A Key")]
    public async Task Refuses_a_category_the_app_does_not_offer_or_a_malformed_answer(string category, string answer)
    {
        Mock<IOnboardingSetupDraftStore> drafts = new(MockBehavior.Strict);

        SaveOnboardingSetupDraftOutcome outcome = await new SaveOnboardingSetupDraftCommandHandler(drafts.Object, Context("user-1"), Profile)
            .Handle(new(category, [answer]), default);

        outcome.Should().Be(SaveOnboardingSetupDraftOutcome.Invalid);
    }

    [Fact]
    public async Task Refuses_without_a_signed_in_person()
    {
        SaveOnboardingSetupDraftOutcome outcome = await new SaveOnboardingSetupDraftCommandHandler(new Mock<IOnboardingSetupDraftStore>(MockBehavior.Strict).Object, Context(null), Profile)
            .Handle(new("dentistry", []), default);

        outcome.Should().Be(SaveOnboardingSetupDraftOutcome.NotSignedIn);
    }

    [Fact]
    public async Task Drops_a_saved_category_the_app_no_longer_offers()
    {
        Mock<IOnboardingSetupDraftStore> drafts = new();
        drafts.Setup(store => store.GetAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(new OnboardingSetupDraft("retired", []));

        OnboardingSetupDraft? draft = await new GetOnboardingSetupDraftQueryHandler(drafts.Object, Context("user-1"), Profile).Handle(new(), default);

        draft.Should().BeNull();
    }

    [Fact]
    public async Task The_profile_store_round_trips_a_draft_under_its_own_key_and_ignores_a_corrupt_value()
    {
        Dictionary<string, string> profile = new() { ["Culture"] = "fa" };
        Mock<IUserProfileService> profiles = new();
        profiles.Setup(service => service.GetProfileAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(() => profile);
        profiles.Setup(service => service.SetValueAsync("user-1", ProfileOnboardingSetupDraftStore.ProfileKey, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, CancellationToken>((_, key, value, _) => profile[key] = value!)
            .ReturnsAsync(SetUserProfileValueOutcome.Saved);
        ProfileOnboardingSetupDraftStore store = new(profiles.Object);

        (await store.SaveAsync("user-1", new OnboardingSetupDraft("dentistry", ["phone"]))).Should().BeTrue();
        (await store.GetAsync("user-1")).Should().BeEquivalentTo(new OnboardingSetupDraft("dentistry", ["phone"]));

        profile[ProfileOnboardingSetupDraftStore.ProfileKey] = "{not json";
        (await store.GetAsync("user-1")).Should().BeNull();
        profile["Culture"].Should().Be("fa");
    }

    private static IRequestContext Context(string? userId) => Mock.Of<IRequestContext>(context => context.UserId == userId);
}
