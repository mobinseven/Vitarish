using System.Threading.Channels;
using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public sealed class InMemoryEntityIndexingMessageQueue : IEntityIndexingMessageQueue
{
    private readonly Channel<EntityIndexingMessage> _channel = Channel.CreateUnbounded<EntityIndexingMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(EntityIndexingMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<EntityIndexingMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
