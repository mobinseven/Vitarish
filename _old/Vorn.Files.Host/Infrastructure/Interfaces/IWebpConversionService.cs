namespace Vorn.Files.Host.Infrastructure.Interfaces;

public interface IWebpConversionService
{
    void Enqueue(string filePath, string contentType);
}
