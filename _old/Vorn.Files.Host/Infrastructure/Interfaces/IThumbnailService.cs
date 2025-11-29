namespace Vorn.Files.Host.Infrastructure.Interfaces;

public interface IThumbnailService
{
    void Enqueue(string filePath, string contentType);
}
