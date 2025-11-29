namespace Vorn.Aaas.Client.Api.Abstractions;

public interface IRequestIdentityContextWriter
{
    void Write(RequestIdentityContextCommand command);
}
