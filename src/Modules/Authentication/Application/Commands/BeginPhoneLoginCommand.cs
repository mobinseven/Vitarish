namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginPhoneLoginCommand(string PhoneNumber, string? ReturnUrl);
