using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Vitarish.Sea.Api.Authorization;

public static class SeaAuthorizationOperations
{
    public static OperationAuthorizationRequirement Read { get; } = new() { Name = nameof(Read) };
    public static OperationAuthorizationRequirement Write { get; } = new() { Name = nameof(Write) };
    public static OperationAuthorizationRequirement Manage { get; } = new() { Name = nameof(Manage) };
}

public static class SeaAuthorizationPolicies
{
    public const string Manage = "Sea.Manage";
}
