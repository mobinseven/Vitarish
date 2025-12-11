using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Queries;
/// <summary>
/// Query for all API clients.
/// </summary>
public sealed record GetApiClients : IRequest<IEnumerable<ApiClient>>;
