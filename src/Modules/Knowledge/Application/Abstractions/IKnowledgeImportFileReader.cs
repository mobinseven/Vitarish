using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeImportFileReader
{
    Task<KnowledgeImportFile> OpenAsync(Guid fileInformationId, string tenantId, CancellationToken cancellationToken);
}
