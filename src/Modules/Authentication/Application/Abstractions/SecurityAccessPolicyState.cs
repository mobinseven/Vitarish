using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Abstractions;

public sealed record SecurityAccessPolicyState(SecurityAccessPolicy Policy, long Version);
