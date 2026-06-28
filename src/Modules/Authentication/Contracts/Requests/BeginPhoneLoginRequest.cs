namespace Vitarish.Authentication.Contracts.Requests;

public sealed record BeginPhoneLoginRequest(string PhoneNumber, string? ReturnUrl);
