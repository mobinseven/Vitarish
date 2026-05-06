using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record GetIncompleteVisitSummaryQuery(string TenantId) : IRequest<IncompleteVisitSummary>;
