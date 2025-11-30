namespace Vorn.Files.Host.Infrastructure.Interfaces;

public interface IThumbnailService
{
    void Enqueue(string objectKey, string contentType);
}
