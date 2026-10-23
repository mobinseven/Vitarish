using System.Text.RegularExpressions;
using MediatR;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.Onboarding;

/// <summary>
/// What a person told an app before signing up, kept so clinic setup can start from it: the business
/// category (one of the self-service profile's) and the keys of their other answers, which belong to
/// the app that asked them (ADR-0017).
/// </summary>
public sealed record OnboardingSetupDraft(string Category, IReadOnlyList<string> Answers);

/// <summary>Keeps one setup draft per user.</summary>
public interface IOnboardingSetupDraftStore
{
    Task<OnboardingSetupDraft?> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <returns>False when another change to the user landed first; the caller may try again.</returns>
    Task<bool> SaveAsync(string userId, OnboardingSetupDraft draft, CancellationToken cancellationToken = default);

    Task ClearAsync(string userId, CancellationToken cancellationToken = default);
}

public enum SaveOnboardingSetupDraftOutcome
{
    Saved,
    NotSignedIn,
    Invalid,
    Unavailable,
    Conflict
}

/// <summary>Saves the signed-in person's setup draft, replacing any earlier one.</summary>
public sealed record SaveOnboardingSetupDraftCommand(string Category, IReadOnlyList<string> Answers) : IRequest<SaveOnboardingSetupDraftOutcome>;

/// <summary>The signed-in person's setup draft, or null; a category the app no longer offers is dropped.</summary>
public sealed record GetOnboardingSetupDraftQuery : IRequest<OnboardingSetupDraft?>;

public static partial class OnboardingSetupDraftRules
{
    public const int MaxAnswers = 8;
    public const int MaxAnswerLength = 40;

    public static bool AreValidAnswers(IReadOnlyList<string> answers) =>
        answers.Count <= MaxAnswers
        && answers.All(answer => answer.Length <= MaxAnswerLength && AnswerKey().IsMatch(answer));

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex AnswerKey();
}

public sealed class SaveOnboardingSetupDraftCommandHandler(
    IOnboardingSetupDraftStore drafts,
    IRequestContext requestContext,
    SelfServiceTenantProfile? profile = null)
    : IRequestHandler<SaveOnboardingSetupDraftCommand, SaveOnboardingSetupDraftOutcome>
{
    public async Task<SaveOnboardingSetupDraftOutcome> Handle(SaveOnboardingSetupDraftCommand command, CancellationToken cancellationToken)
    {
        if (profile is null)
            return SaveOnboardingSetupDraftOutcome.Unavailable;
        if (string.IsNullOrWhiteSpace(requestContext.UserId))
            return SaveOnboardingSetupDraftOutcome.NotSignedIn;
        if (!profile.AllowsCategory(command.Category) || !OnboardingSetupDraftRules.AreValidAnswers(command.Answers))
            return SaveOnboardingSetupDraftOutcome.Invalid;

        return await drafts.SaveAsync(requestContext.UserId, new OnboardingSetupDraft(command.Category, command.Answers.Distinct(StringComparer.Ordinal).ToArray()), cancellationToken)
            ? SaveOnboardingSetupDraftOutcome.Saved
            : SaveOnboardingSetupDraftOutcome.Conflict;
    }
}

public sealed class GetOnboardingSetupDraftQueryHandler(
    IOnboardingSetupDraftStore drafts,
    IRequestContext requestContext,
    SelfServiceTenantProfile? profile = null)
    : IRequestHandler<GetOnboardingSetupDraftQuery, OnboardingSetupDraft?>
{
    public async Task<OnboardingSetupDraft?> Handle(GetOnboardingSetupDraftQuery query, CancellationToken cancellationToken)
    {
        if (profile is null || string.IsNullOrWhiteSpace(requestContext.UserId))
            return null;
        OnboardingSetupDraft? draft = await drafts.GetAsync(requestContext.UserId, cancellationToken);
        return draft is not null && profile.AllowsCategory(draft.Category) ? draft : null;
    }
}
