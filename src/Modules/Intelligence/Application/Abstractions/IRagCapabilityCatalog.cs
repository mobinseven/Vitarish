using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagCapabilityCatalog
{
    IReadOnlyList<RagCapabilityDescriptor> GetCapabilities();
}
