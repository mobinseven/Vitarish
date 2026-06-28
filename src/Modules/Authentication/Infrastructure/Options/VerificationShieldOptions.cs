using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class VerificationShieldOptions { public int MaximumRequests { get; set; } = 3; public int WindowMinutes { get; set; } = 2; }
