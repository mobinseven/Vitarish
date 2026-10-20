using MediatR;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;

namespace Vitarish.Journey.Infrastructure.Host;

/// <summary>Asks Parties whether the request came to the onboarding host.</summary>
public sealed class PartiesJourneyHostReader(ISender sender) : IJourneyHostReader
{
    public Task<bool> IsSignUpHostAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new IsOnboardingHostQuery(), cancellationToken);
}
