using MediatR;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;

namespace Vitarish.Journey.Infrastructure.Host;

/// <summary>Asks Parties which platform host the request came to.</summary>
public sealed class PartiesJourneyHostReader(ISender sender) : IJourneyHostReader
{
    public Task<bool> IsSignUpHostAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new IsOnboardingHostQuery(), cancellationToken);

    public Task<bool> IsPlatformAdminHostAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new IsPlatformAdminHostQuery(), cancellationToken);

    public Task<bool> IsSignInHostAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new IsSignInHostQuery(), cancellationToken);
}
