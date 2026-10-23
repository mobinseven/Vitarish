using FluentAssertions;
using Moq;
using Vitarish.Intelligence.Application.Memory;
using Vitarish.Intelligence.Infrastructure.Services.Memory;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;

namespace Intelligence.Tests.Infrastructure;

public sealed class ProfileUserMemoryStoreTests
{
    private static readonly DateTimeOffset RememberedAt = new(2026, 10, 23, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Writes_only_over_the_version_it_read_and_keeps_persian_text_as_written()
    {
        Dictionary<string, string> profile = [];
        ProfileValueExpectation? expected = null;
        Mock<IUserProfileService> profiles = Profiles(profile, value => expected = value);
        ProfileUserMemoryStore store = new(profiles.Object);

        UserMemorySnapshot empty = await store.GetAsync("user-1");
        (await store.ReplaceAsync("user-1", [Item("a", "کلینیک دندانپزشکی دارد")], empty.Version)).Should().BeTrue();

        expected.Should().Be(new ProfileValueExpectation(null));
        profile[ProfileUserMemoryStore.ProfileKey].Should().Contain("کلینیک دندانپزشکی دارد");
        UserMemorySnapshot read = await store.GetAsync("user-1");
        read.Items.Select(item => item.Text).Should().Equal("کلینیک دندانپزشکی دارد");
        read.Version.Should().Be(profile[ProfileUserMemoryStore.ProfileKey]);
    }

    [Fact]
    public async Task Drops_the_oldest_items_when_the_memory_does_not_fit_one_profile_entry()
    {
        Dictionary<string, string> profile = [];
        ProfileUserMemoryStore store = new(Profiles(profile, _ => { }).Object);
        UserMemoryItem[] items = Enumerable.Range(0, UserMemoryRules.MaxItems)
            .Select(index => Item($"item-{index:D2}", new string('ک', UserMemoryRules.MaxTextLength)))
            .ToArray();

        (await store.ReplaceAsync("user-1", items, null)).Should().BeTrue();

        profile[ProfileUserMemoryStore.ProfileKey].Length.Should().BeLessThanOrEqualTo(SetUserProfileValueCommand.MaxValueLength);
        IReadOnlyList<UserMemoryItem> kept = (await store.GetAsync("user-1")).Items;
        kept.Should().NotBeEmpty().And.HaveCountLessThan(items.Length);
        kept[^1].Id.Should().Be(items[^1].Id);
    }

    private static Mock<IUserProfileService> Profiles(Dictionary<string, string> profile, Action<ProfileValueExpectation?> expectation)
    {
        Mock<IUserProfileService> profiles = new();
        profiles.Setup(service => service.GetProfileAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(() => profile);
        profiles.Setup(service => service.SetValueAsync("user-1", ProfileUserMemoryStore.ProfileKey, It.IsAny<string?>(), It.IsAny<ProfileValueExpectation?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, ProfileValueExpectation?, CancellationToken>((_, key, value, expected, _) =>
            {
                expectation(expected);
                if (value is null)
                    _ = profile.Remove(key);
                else
                    profile[key] = value;
            })
            .ReturnsAsync(SetUserProfileValueOutcome.Saved);
        return profiles;
    }

    private static UserMemoryItem Item(string id, string text) => new(id, text, UserMemorySource.Landing, RememberedAt);
}
