using System.Text.RegularExpressions;
using MediatR;

namespace Vitarish.Intelligence.Application.Memory;

/// <summary>
/// Where a remembered fact came from. Only conversations about the person and their business may add
/// to memory; clinic conversations, which hold patient data, have no source here on purpose (ADR-0017).
/// </summary>
public enum UserMemorySource
{
    Landing,
    Onboarding
}

/// <summary>One thing Vitarish remembers about a person, which they can see, correct or delete.</summary>
public sealed record UserMemoryItem(string Id, string Text, UserMemorySource Source, DateTimeOffset RememberedAt);

/// <summary>Keeps one person's memory.</summary>
public interface IUserMemoryStore
{
    Task<IReadOnlyList<UserMemoryItem>> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <returns>False when another change to the person landed first; the caller may read again and retry.</returns>
    Task<bool> ReplaceAsync(string userId, IReadOnlyList<UserMemoryItem> items, CancellationToken cancellationToken = default);
}

public static partial class UserMemoryRules
{
    public const int MaxItems = 50;
    public const int MaxTextLength = 280;
    public const int MaxFactsPerTurn = 3;

    /// <summary>
    /// Business context only: text with a long run of digits (a national ID, a phone or file number) is
    /// refused, as a guard against storing details about patients or other people.
    /// </summary>
    public static bool IsStorable(string? text) =>
        text is { Length: > 0 and <= MaxTextLength } && !string.IsNullOrWhiteSpace(text) && !LongDigitRun().IsMatch(text);

    [GeneratedRegex(@"\d[\d\s-]{6,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex LongDigitRun();
}

/// <summary>Adds facts an assistant learned; returns only the items actually added.</summary>
public sealed record RememberUserFactsCommand(string UserId, IReadOnlyList<string> Facts, UserMemorySource Source) : IRequest<IReadOnlyList<UserMemoryItem>>;

public sealed record GetUserMemoryQuery(string UserId) : IRequest<IReadOnlyList<UserMemoryItem>>;

/// <summary>Corrects one item's wording; false when the item does not exist or the text is not storable.</summary>
public sealed record UpdateUserMemoryItemCommand(string UserId, string ItemId, string Text) : IRequest<bool>;

/// <summary>Deletes one item, or everything when <paramref name="ItemId"/> is null; false when the item does not exist.</summary>
public sealed record ForgetUserMemoryCommand(string UserId, string? ItemId) : IRequest<bool>;

public sealed class UserMemoryHandlers(IUserMemoryStore store, TimeProvider timeProvider) :
    IRequestHandler<RememberUserFactsCommand, IReadOnlyList<UserMemoryItem>>,
    IRequestHandler<GetUserMemoryQuery, IReadOnlyList<UserMemoryItem>>,
    IRequestHandler<UpdateUserMemoryItemCommand, bool>,
    IRequestHandler<ForgetUserMemoryCommand, bool>
{
    private const int Attempts = 3;

    public async Task<IReadOnlyList<UserMemoryItem>> Handle(RememberUserFactsCommand request, CancellationToken cancellationToken)
    {
        string[] facts = request.Facts.Select(fact => fact.Trim()).Where(UserMemoryRules.IsStorable).Take(UserMemoryRules.MaxFactsPerTurn).ToArray();
        if (facts.Length == 0)
            return [];

        IReadOnlyList<UserMemoryItem> added = [];
        bool saved = await ChangeAsync(request.UserId, current =>
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            added = facts
                .Where(fact => !current.Any(item => string.Equals(item.Text, fact, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(fact => new UserMemoryItem(Guid.NewGuid().ToString("N"), fact, request.Source, now))
                .ToArray();
            // The newest facts win once memory is full.
            return added.Count == 0 ? null : current.Concat(added).TakeLast(UserMemoryRules.MaxItems).ToArray();
        }, cancellationToken);
        return saved ? added : [];
    }

    public Task<IReadOnlyList<UserMemoryItem>> Handle(GetUserMemoryQuery request, CancellationToken cancellationToken) =>
        store.GetAsync(request.UserId, cancellationToken);

    public async Task<bool> Handle(UpdateUserMemoryItemCommand request, CancellationToken cancellationToken)
    {
        string text = request.Text.Trim();
        if (!UserMemoryRules.IsStorable(text))
            return false;
        bool found = false;
        bool saved = await ChangeAsync(request.UserId, current =>
        {
            found = current.Any(item => item.Id == request.ItemId);
            return found ? current.Select(item => item.Id == request.ItemId ? item with { Text = text } : item).ToArray() : null;
        }, cancellationToken);
        return found && saved;
    }

    public async Task<bool> Handle(ForgetUserMemoryCommand request, CancellationToken cancellationToken)
    {
        bool found = false;
        bool saved = await ChangeAsync(request.UserId, current =>
        {
            found = request.ItemId is null || current.Any(item => item.Id == request.ItemId);
            return !found ? null : request.ItemId is null ? [] : current.Where(item => item.Id != request.ItemId).ToArray();
        }, cancellationToken);
        return found && saved;
    }

    /// <summary>
    /// Reads, changes and writes the person's memory, retrying when another change landed first. The
    /// change returns null when there is nothing to write.
    /// </summary>
    private async Task<bool> ChangeAsync(string userId, Func<IReadOnlyList<UserMemoryItem>, IReadOnlyList<UserMemoryItem>?> change, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < Attempts; attempt++)
        {
            IReadOnlyList<UserMemoryItem> current = await store.GetAsync(userId, cancellationToken);
            IReadOnlyList<UserMemoryItem>? next = change(current);
            if (next is null)
                return true;
            if (await store.ReplaceAsync(userId, next, cancellationToken))
                return true;
        }

        return false;
    }
}
