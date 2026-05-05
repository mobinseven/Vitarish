using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetAccessPolicyQueryHandler(IClinicalAccessPolicyService policyService)
    : IRequestHandler<GetAccessPolicyQuery, ClinicalAccessPolicy>
{
    public Task<ClinicalAccessPolicy> Handle(GetAccessPolicyQuery request, CancellationToken cancellationToken) =>
        policyService.GetPolicyAsync(cancellationToken);
}
