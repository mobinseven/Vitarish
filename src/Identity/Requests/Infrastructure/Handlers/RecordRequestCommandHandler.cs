using MediatR;
using Vitarish.Requests.Application.Commands;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Requests.Infrastructure.Data;

namespace Vitarish.Requests.Infrastructure.Handlers;

public sealed class RecordRequestCommandHandler(RequestsDbContext dbContext) : IRequestHandler<RecordRequestCommand>
{
    public async Task Handle(RecordRequestCommand request, CancellationToken cancellationToken)
    {
        Request? entity = new()
        {
            RequestId = request.RequestId,
            Authority = request.Authority,
            ClientId = request.ClientId,
            OwnerId = request.OwnerId,
            TenantId = request.TenantId,
            UserId = request.UserId,
            ResourceId = request.ResourceId,
            TraceId = request.TraceId,
            AdditionalInfo = request.AdditionalInfo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _ = await dbContext.Requests.AddAsync(entity, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
