using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Keeps the setup draft in the user's profile under a Parties-owned key, so it follows the person from
/// the landing host to the onboarding host without a table of its own (ADR-0017).
/// </summary>
public sealed class ProfileOnboardingSetupDraftStore(IUserProfileService profiles) : IOnboardingSetupDraftStore
{
    public const string ProfileKey = "parties.onboarding.setup-draft";
    private const string CategoryProperty = "category";
    private const string AnswersProperty = "answers";

    public async Task<OnboardingSetupDraft?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> profile = await profiles.GetProfileAsync(userId, cancellationToken) ?? [];
        return profile.TryGetValue(ProfileKey, out string? stored) ? Read(stored) : null;
    }

    public async Task<bool> SaveAsync(string userId, OnboardingSetupDraft draft, CancellationToken cancellationToken = default)
    {
        JsonArray answers = [];
        foreach (string answer in draft.Answers)
            answers.Add(answer);
        string value = new JsonObject { [CategoryProperty] = draft.Category, [AnswersProperty] = answers }.ToJsonString();
        return await profiles.SetValueAsync(userId, ProfileKey, value, cancellationToken) == SetUserProfileValueOutcome.Saved;
    }

    public Task ClearAsync(string userId, CancellationToken cancellationToken = default) =>
        profiles.SetValueAsync(userId, ProfileKey, null, cancellationToken);

    // A stored value that does not parse is treated as no draft; it never blocks setup.
    private static OnboardingSetupDraft? Read(string stored)
    {
        try
        {
            if (JsonNode.Parse(stored) is not JsonObject root || root[CategoryProperty]?.GetValue<string>() is not { Length: > 0 } category)
                return null;
            string[] answers = root[AnswersProperty] is JsonArray array
                ? array.Select(item => item?.GetValue<string>()).OfType<string>().ToArray()
                : [];
            return new OnboardingSetupDraft(category, answers);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}
