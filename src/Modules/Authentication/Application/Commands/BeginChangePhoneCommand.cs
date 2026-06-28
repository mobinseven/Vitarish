namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginChangePhoneCommand(string UserId, string PhoneNumber);
