using MediatR;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Extensions;
using Vitarish.Knowledge.Infrastructure.Validators;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class SaveKnowledgeImportCommandHandler(KnowledgeDbContext dbContext)
    : IRequestHandler<SaveKnowledgeImportCommand>
{
    public async Task Handle(
        SaveKnowledgeImportCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        KnowledgeImportResult result = command.Result;

        ArgumentNullException.ThrowIfNull(result);
        KnowledgeImportRules.ValidateImport(result);

        string tenantId = result.Sources[0].TenantId;

        await dbContext.EnsureCorpusExistsAsync(
            result.CorpusId,
            tenantId,
            cancellationToken);

        dbContext.StageImport(result);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
