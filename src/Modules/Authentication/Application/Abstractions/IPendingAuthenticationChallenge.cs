using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>Protected browser reference; never stores credentials or verification codes.</summary>
public interface IPendingAuthenticationChallenge
{
    void Store(ChallengeResult challenge, string? returnUrl);
    PendingChallengeReference? Read();
    void Clear();
}

public sealed record PendingChallengeReference(ChallengeResult Challenge, string? ReturnUrl);
