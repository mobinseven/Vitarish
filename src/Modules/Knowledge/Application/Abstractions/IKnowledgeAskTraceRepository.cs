using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeAskTraceRepository
{
    Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken);
}
