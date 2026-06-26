namespace Vitarish.Users.Contracts.Requests;

public sealed record CheckUserPhoneNumberInUseQueryParameters(string PhoneNumber, string? ExcludeUserId = null);
