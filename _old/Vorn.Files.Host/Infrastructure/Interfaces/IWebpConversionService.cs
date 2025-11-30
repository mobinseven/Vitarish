namespace Vorn.Files.Host.Infrastructure.Interfaces;

public interface IWebpConversionService
{
    void Enqueue(string objectKey, string contentType);
}
