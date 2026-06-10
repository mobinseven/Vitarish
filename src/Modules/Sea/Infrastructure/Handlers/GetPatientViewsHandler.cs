using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Queries.Patients;
using Vitarish.Sea.Infrastructure.ReadModels;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal sealed class GetPatientViewsHandler(
    SeaDbContext dbContext,
    IRequestContext requestContext,
    EntitySearchPatternFactory searchPatternFactory) : IRequestHandler<GetPatientViewsQuery, IReadOnlyList<PatientListItemDto>>
{
    public async Task<IReadOnlyList<PatientListItemDto>> Handle(GetPatientViewsQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");

        IQueryable<PatientView> query = dbContext.PatientViews
            .AsNoTracking()
            .Where(patient => patient.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            IReadOnlyCollection<string> patterns = searchPatternFactory.CreateSearchPatterns(request.Search);
            foreach (string pattern in patterns)
            {
                query = query.Where(patient =>
                    EF.Functions.ILike(patient.Name, pattern)
                    || EF.Functions.ILike(patient.Description, pattern)
                    || (patient.IdentificationCode != null && EF.Functions.ILike(patient.IdentificationCode, pattern))
                    || (patient.FirstName != null && EF.Functions.ILike(patient.FirstName, pattern))
                    || (patient.LastName != null && EF.Functions.ILike(patient.LastName, pattern))
                    || (patient.Phone != null && EF.Functions.ILike(patient.Phone, pattern))
                    || (patient.Email != null && EF.Functions.ILike(patient.Email, pattern)));
            }
        }

        query = query.OrderBy(patient => patient.LastName ?? patient.Name)
            .ThenBy(patient => patient.FirstName)
            .ThenBy(patient => patient.Name)
            .ThenBy(patient => patient.Id);

        if (request.Skip is > 0)
        {
            query = query.Skip(request.Skip.Value);
        }

        if (request.Take is > 0)
        {
            query = query.Take(request.Take.Value);
        }

        return await query.Select(patient => new PatientListItemDto(
                patient.Id,
                patient.TenantId,
                patient.Name,
                patient.Description,
                patient.IdentificationCode,
                patient.CreatedAt,
                patient.CreatedBy,
                patient.UpdatedAt,
                patient.UpdatedBy,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Phone,
                patient.Email))
            .ToListAsync(cancellationToken);
    }
}
