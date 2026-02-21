using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public interface IEntityIndexingMessageQueue
{
    ValueTask EnqueueAsync(EntityIndexingMessage message, CancellationToken cancellationToken = default);

    IAsyncEnumerable<EntityIndexingMessage> ReadAllAsync(CancellationToken cancellationToken);
}
