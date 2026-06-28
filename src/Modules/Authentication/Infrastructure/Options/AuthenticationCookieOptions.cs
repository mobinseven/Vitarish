using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationCookieOptions { public string ReturnUrlCookieName { get; set; } = "Authentication.ReturnUrl"; }
