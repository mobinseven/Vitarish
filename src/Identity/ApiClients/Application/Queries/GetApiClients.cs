
using FluentValidation;
using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Queries;
/// <summary>
/// Query for all API clients.
/// </summary>
public sealed record GetApiClients : IRequest<IEnumerable<ApiClient>>;

/// <summary>
/// Validator for <see cref="GetApiClients"/>.
/// </summary>
public sealed class GetApiClientsValidator : AbstractValidator<GetApiClients> { }
