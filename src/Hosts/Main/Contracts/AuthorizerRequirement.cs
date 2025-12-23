using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Main.Contracts;

public record AuthorizerRequirement(string Role) : IAuthorizationRequirement;
