using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationChallengeOptions { public int LifetimeMinutes { get; set; } = 5; public int MaximumAttempts { get; set; } = 5; }
