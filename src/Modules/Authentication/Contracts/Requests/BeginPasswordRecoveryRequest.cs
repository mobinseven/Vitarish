using Vitarish.Authentication.Contracts.Enums;

namespace Vitarish.Authentication.Contracts.Requests;

public sealed record BeginPasswordRecoveryRequest(PasswordRecoveryChannelDto Channel, string Contact);
