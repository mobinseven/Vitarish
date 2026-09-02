using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Commands;

public sealed record ChangeSecurityAccessPolicyCommand(SecurityAccessPolicy Policy, long ExpectedVersion);
