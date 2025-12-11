using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface IResponder
{
    Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default);
    IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default);
}
