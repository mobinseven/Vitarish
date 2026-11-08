namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string ContinuationBeginPath = "/continuations";
    public const string ContinuationCompletePath = ContinuationBeginPath + "/complete";
}

public static class AuthenticationContinuationOperationIds
{
    public const string Begin = "Authentication_Continuation_Begin";
    public const string Complete = "Authentication_Continuation_Complete";
}
