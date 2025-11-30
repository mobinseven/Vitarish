using Vorn.Aaas.Client.Api.Contracts;

namespace Vorn.Aaas.Client.Api.Abstractions;

public interface IRequestIdentityContextGenerator
{
    Task Generate(ScopeRequestIdentity? request = null);
}
