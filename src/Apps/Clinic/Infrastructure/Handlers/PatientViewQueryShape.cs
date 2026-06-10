using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Entities.Server.Infrastructure.Search;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class PatientViewQueryShape(EntitySearchPatternFactory searchPatternFactory)
{
    public IQueryable<PatientView> ApplyFilters(
        IQueryable<PatientView> query,
        string tenantId,
        string? search,
        bool? template)
    {
        query = query.Where(patient => patient.TenantId == tenantId);

        if (template is not null)
        {
            query = query.Where(patient => patient.Template == template);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            IReadOnlyCollection<string> patterns = searchPatternFactory.CreateSearchPatterns(search);
            foreach (string pattern in patterns)
            {
                query = query.Where(patient =>
                    EF.Functions.ILike(patient.Name, pattern)
                    || EF.Functions.ILike(patient.Description, pattern)
                    || EF.Functions.ILike(patient.IdentificationCode!, pattern)
                    || EF.Functions.ILike(patient.FirstName!, pattern)
                    || EF.Functions.ILike(patient.LastName!, pattern)
                    || EF.Functions.ILike(patient.Email!, pattern)
                    || EF.Functions.ILike(patient.Phone!, pattern));
            }
        }

        return query;
    }

    public IQueryable<PatientView> ApplySort(
        IQueryable<PatientView> query,
        ProjectSortBy? sortBy,
        bool descending)
    {
        sortBy ??= ProjectSortBy.Name;

        return (sortBy, descending) switch
        {
            (ProjectSortBy.Name, true) => query
                .OrderByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Name)
                .ThenByDescending(patient => patient.Id),
            (ProjectSortBy.Name, false) => query
                .OrderBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Name)
                .ThenBy(patient => patient.Id),
            (ProjectSortBy.Template, true) => query
                .OrderByDescending(patient => patient.Template)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (ProjectSortBy.Template, false) => query
                .OrderBy(patient => patient.Template)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            _ => query
                .OrderBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Name)
                .ThenBy(patient => patient.Id)
        };
    }
}
