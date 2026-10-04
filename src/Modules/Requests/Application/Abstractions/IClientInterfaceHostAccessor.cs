namespace Vitarish.Requests.Application.Abstractions;

/// <summary>Provides the validated browser-facing authority for the current request, when available.</summary>
public interface IClientInterfaceHostAccessor
{
    string? ClientInterfaceHost { get; }
}
