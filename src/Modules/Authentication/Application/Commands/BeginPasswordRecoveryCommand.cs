using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginPasswordRecoveryCommand(PasswordRecoveryChannel Channel, string Contact);
