using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record GetTenantClinicalSettingsQuery : IRequest<ClinicalTenantSettings>;
