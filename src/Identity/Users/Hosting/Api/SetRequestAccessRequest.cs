using Vitarish.Requests.Domain.Models;

namespace Vitarish.Users.Hosting.Api;

/// <summary>
/// Request payload for setting access using the request pattern.
/// </summary>
public sealed record SetRequestAccessRequest(RequestIdentityDto Request, string AccessValue);
