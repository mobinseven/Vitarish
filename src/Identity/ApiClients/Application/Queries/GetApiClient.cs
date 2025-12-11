using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="ApiClient"/> by identifier.
/// </summary>
public sealed record GetApiClient(string Id) : IRequest<ApiClient?>;
