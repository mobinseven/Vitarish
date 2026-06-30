using MediatR;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Queries;

public sealed record KnowledgeUserWorkspaceQuery(string? TenantId, string? UserId) : IRequest<KnowledgeUserWorkspaceReadModel>;
