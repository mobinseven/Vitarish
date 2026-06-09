using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IConnectionMemorySampler
{
    ProcessMemorySample Sample();
}
