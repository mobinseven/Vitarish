using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformConnectionMemoryCommand
{
    Task RecordAsync(
        RecordConnectionMemorySampleRequest request,
        CancellationToken cancellationToken);
}
