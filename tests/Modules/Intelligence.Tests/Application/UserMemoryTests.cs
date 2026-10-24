using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.Memory;

namespace Intelligence.Tests.Application;

public sealed class UserMemoryTests
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 23, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Remembers_new_facts_once_and_skips_ones_it_already_knows()
    {
        InMemoryStore store = new(Item("a", "Runs a dental clinic"));

        IReadOnlyList<UserMemoryItem> added = await Handlers(store).Handle(
            new RememberUserFactsCommand("user-1", ["runs a dental clinic", "Patients book by phone", "Patients book by phone"], UserMemorySource.Landing), default);

        added.Select(item => item.Text).Should().Equal("Patients book by phone");
        store.Items.Select(item => item.Text).Should().Equal("Runs a dental clinic", "Patients book by phone");
    }

    [Theory]
    [InlineData("Patient national ID 0012345678")]
    [InlineData("Call me on 0912 345 6789")]
    [InlineData("   ")]
    public async Task Refuses_numbers_and_empty_text_as_a_guard_against_personal_details(string fact)
    {
        InMemoryStore store = new();

        IReadOnlyList<UserMemoryItem> added = await Handlers(store).Handle(new RememberUserFactsCommand("user-1", [fact], UserMemorySource.Landing), default);

        added.Should().BeEmpty();
        store.Writes.Should().Be(0);
    }

    [Fact]
    public async Task Keeps_the_newest_facts_when_memory_is_full()
    {
        InMemoryStore store = new(Enumerable.Range(0, UserMemoryRules.MaxItems).Select(index => Item($"old-{index}", $"Fact {index}")).ToArray());

        _ = await Handlers(store).Handle(new RememberUserFactsCommand("user-1", ["Brand new fact"], UserMemorySource.Landing), default);

        store.Items.Should().HaveCount(UserMemoryRules.MaxItems);
        store.Items[^1].Text.Should().Be("Brand new fact");
        store.Items.Should().NotContain(item => item.Id == "old-0");
    }

    [Fact]
    public async Task Retries_when_another_change_landed_first()
    {
        InMemoryStore store = new() { ConflictsLeft = 1 };

        IReadOnlyList<UserMemoryItem> added = await Handlers(store).Handle(new RememberUserFactsCommand("user-1", ["Two providers"], UserMemorySource.Landing), default);

        added.Should().ContainSingle();
        store.Items.Should().ContainSingle(item => item.Text == "Two providers");
    }

    [Fact]
    public async Task Corrects_forgets_one_or_forgets_everything_and_reports_a_missing_item()
    {
        InMemoryStore store = new(Item("a", "Runs a dental clinic"), Item("b", "Books by phone"));
        UserMemoryHandlers handlers = Handlers(store);

        (await handlers.Handle(new UpdateUserMemoryItemCommand("user-1", "a", "Runs a dermatology clinic"), default)).Should().BeTrue();
        (await handlers.Handle(new ForgetUserMemoryCommand("user-1", "b"), default)).Should().BeTrue();
        (await handlers.Handle(new ForgetUserMemoryCommand("user-1", "missing"), default)).Should().BeFalse();
        store.Items.Select(item => item.Text).Should().Equal("Runs a dermatology clinic");

        (await handlers.Handle(new ForgetUserMemoryCommand("user-1", null), default)).Should().BeTrue();
        store.Items.Should().BeEmpty();
    }

    [Fact]
    public void The_reply_format_reads_facts_to_remember_only_for_apps_that_keep_memory()
    {
        const string json = """{"text":"Noted.","question":null,"references":[],"nextAction":null,"remember":["Runs a dental clinic","a","b","c"]}""";
        using JsonDocument document = JsonDocument.Parse(json);
        AgentRunResult run = new(true, json, document.RootElement.Clone(), new(1, 1), TimeSpan.Zero, 1, 0);

        AgentInteractiveReplyFormat.Read(run, new([], [], AllowsMemory: true))!.Remember.Should().HaveCount(AgentInteractiveReplyFormat.MaxRemember);
        AgentInteractiveReplyFormat.Read(run, new([], []))!.Remember.Should().BeEmpty();
        AgentInteractiveReplyFormat.Definition(new([], [], AllowsMemory: true)).JsonSchema.Should().Contain(AgentInteractiveReplyFormat.RememberProperty);
        AgentInteractiveReplyFormat.Definition(new([], [])).JsonSchema.Should().NotContain(AgentInteractiveReplyFormat.RememberProperty);
    }

    private UserMemoryHandlers Handlers(IUserMemoryStore store) => new(store, clock);

    private UserMemoryItem Item(string id, string text) => new(id, text, UserMemorySource.Landing, clock.GetUtcNow());

    private sealed class InMemoryStore(params UserMemoryItem[] items) : IUserMemoryStore
    {
        public List<UserMemoryItem> Items { get; private set; } = [.. items];
        public int ConflictsLeft { get; set; }
        public int Writes { get; private set; }

        public Task<UserMemorySnapshot> GetAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserMemorySnapshot(Items.ToArray(), Writes.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        public Task<bool> ReplaceAsync(string userId, IReadOnlyList<UserMemoryItem> items, string? expectedVersion, CancellationToken cancellationToken = default)
        {
            if (ConflictsLeft-- > 0)
            {
                // Another writer landed first, so the version just read is stale.
                Writes++;
                return Task.FromResult(false);
            }
            if (expectedVersion != Writes.ToString(System.Globalization.CultureInfo.InvariantCulture))
                return Task.FromResult(false);
            Writes++;
            Items = [.. items];
            return Task.FromResult(true);
        }
    }
}
