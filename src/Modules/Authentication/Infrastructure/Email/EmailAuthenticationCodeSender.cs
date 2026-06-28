using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure;

public sealed class EmailAuthenticationCodeSender : IAuthenticationCodeSender { public Task SendAsync(AuthenticationChallengePurpose purpose, string destination, string code, CancellationToken cancellationToken) => Task.CompletedTask; }
