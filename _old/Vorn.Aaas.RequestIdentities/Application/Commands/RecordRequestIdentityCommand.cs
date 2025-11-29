namespace Vorn.Aaas.RequestIdentities.Application.Commands;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.RequestIdentities.Domain.Entities;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;

public sealed record RecordRequestIdentityCommand(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo) : IRequest;

public sealed class RecordRequestIdentityCommandValidator : AbstractValidator<RecordRequestIdentityCommand>
{
    public RecordRequestIdentityCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Authority).NotEmpty();
        RuleFor(command => command.ClientId).NotEmpty();
        RuleFor(command => command.OwnerId).NotEmpty();
    }
}

internal sealed class RecordRequestIdentityCommandHandler(RequestIdentityDbContext dbContext)
    : IRequestHandler<RecordRequestIdentityCommand>
{
    public async Task Handle(RecordRequestIdentityCommand request, CancellationToken cancellationToken)
    {
        //if((request.ResourceId?.StartsWith("/api/users/") ?? false) ||
        //    (request.ResourceId?.StartsWith("/api/apiclients/") ?? false) ||
        //    (request.ResourceId?.StartsWith("/api/authenticationclients/") ?? false)
        //     || (request.ResourceId?.StartsWith("https://") ?? false)
        //     )
        //{
        //    return;
        //}

        RequestIdentity? entity = new()
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

        await dbContext.RequestIdentities.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
