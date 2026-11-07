namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string PendingChallengePath = "/pending-challenge";
    public const string PendingChallenge = BasePath + PendingChallengePath;
}

public static class AuthenticationPendingChallengeOperationIds
{
    public const string Get = "Authentication_PendingChallenge_Get";
    public const string Clear = "Authentication_PendingChallenge_Clear";
}
