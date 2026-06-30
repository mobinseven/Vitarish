namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyReturnUrlPolicy
{
    string? Normalize(string? returnUrl);
}
