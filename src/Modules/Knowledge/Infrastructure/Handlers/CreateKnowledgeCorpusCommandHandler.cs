using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Owner;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class CreateKnowledgeCorpusCommandHandler(IKnowledgeService knowledge, ILogger<CreateKnowledgeCorpusCommandHandler> logger) : IRequestHandler<CreateKnowledgeCorpusCommand, KnowledgeOwnerCreateCorpusResult>
{
    public async Task<KnowledgeOwnerCreateCorpusResult> Handle(CreateKnowledgeCorpusCommand request, CancellationToken cancellationToken)
    {
        string? error = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId) ?? KnowledgeOwnerInputValidator.ValidateCreateCorpus(request.Input);
        if (error is not null)
        {
            return KnowledgeOwnerCreateCorpusResult.Failure(error);
        }

        KnowledgeCorpus corpus = new()
        {
            TenantId = request.TenantId!,
            Name = request.Input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Input.Description) ? null : request.Input.Description.Trim(),
            IsActive = request.Input.IsActive
        };
        try
        {
            await knowledge.SaveCorpusAsync(corpus, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to save knowledge corpus for tenant {TenantId}.", request.TenantId);
            return KnowledgeOwnerCreateCorpusResult.Failure(KnowledgeOwnerFailureMessages.SaveCorpusFailed);
        }

        return KnowledgeOwnerCreateCorpusResult.Success("Corpus created.", corpus.ToOwnerCorpusOption());
    }
}
