using Vorn.Aaas.Client.Api.Contracts;

namespace Vorn.Aaas.Client.Api.Abstractions;

public interface IRequestIdentityContextWriter
{
    Task Write(RequestIdentityContextCommand command);
}
