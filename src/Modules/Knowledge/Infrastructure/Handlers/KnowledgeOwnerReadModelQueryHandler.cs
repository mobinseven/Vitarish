using MediatR;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Owner;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class KnowledgeOwnerReadModelQueryHandler(IKnowledgeService knowledge) : IRequestHandler<KnowledgeOwnerReadModelQuery, KnowledgeOwnerReadModel>
{
    public async Task<KnowledgeOwnerReadModel> Handle(KnowledgeOwnerReadModelQuery request, CancellationToken cancellationToken)
    {
        string? tenantError = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId);
        if (tenantError is not null)
        {
            return KnowledgeOwnerReadModel.Empty;
        }

        IReadOnlyList<KnowledgeCorpus> corpora = await knowledge.ListCorporaAsync(request.TenantId!, cancellationToken);
        IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions = corpora.ToOwnerCorpusOptions();
        KnowledgeOwnerCorpusOption? selected = corpusOptions.ResolveSelectedOwnerCorpus(request.SelectedCorpusId);
        IReadOnlyList<KnowledgeSource> sources = selected is null ? [] : await knowledge.GetSourcesAsync(selected.CorpusId, request.TenantId!, cancellationToken);

        return corpusOptions.ToOwnerReadModel(selected, sources);
    }
}
