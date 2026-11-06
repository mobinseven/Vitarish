namespace Vitarish.Authentication.Contracts.Requests;

public sealed record ChangeTwoStepSignInRequest
{
    public required bool Enabled { get; init; }
}
