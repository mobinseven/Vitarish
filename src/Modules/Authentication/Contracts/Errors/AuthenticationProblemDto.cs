namespace Vitarish.Authentication.Contracts.Errors;

public sealed record AuthenticationProblemDto(AuthenticationErrorCode Code, string Title, string Detail, int Status);
