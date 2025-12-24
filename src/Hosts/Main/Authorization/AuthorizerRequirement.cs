using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Main.Authorization;

public record AuthorizerRequirement(string Role) : IAuthorizationRequirement;
