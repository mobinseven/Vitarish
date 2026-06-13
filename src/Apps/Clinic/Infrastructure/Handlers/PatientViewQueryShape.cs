using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Entities.Server.Infrastructure.Search;

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
        PatientSortBy? sortBy,
        bool descending)
    {
        sortBy ??= PatientSortBy.Name;

        return (sortBy, descending) switch
        {
            (PatientSortBy.Name, true) => query
                .OrderByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Name)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.Name, false) => query
                .OrderBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Name)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.Template, true) => query
                .OrderByDescending(patient => patient.Template)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.Template, false) => query
                .OrderBy(patient => patient.Template)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.IdentificationCode, true) => query
                .OrderByDescending(patient => patient.IdentificationCode)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.IdentificationCode, false) => query
                .OrderBy(patient => patient.IdentificationCode)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.DateOfBirth, true) => query
                .OrderByDescending(patient => patient.DateOfBirth.HasValue)
                .ThenByDescending(patient => patient.DateOfBirth)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.DateOfBirth, false) => query
                .OrderByDescending(patient => patient.DateOfBirth.HasValue)
                .ThenBy(patient => patient.DateOfBirth)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.DateOfSurgery, true) => query
                .OrderByDescending(patient => patient.DateOfSurgery.HasValue)
                .ThenByDescending(patient => patient.DateOfSurgery)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.DateOfSurgery, false) => query
                .OrderByDescending(patient => patient.DateOfSurgery.HasValue)
                .ThenBy(patient => patient.DateOfSurgery)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.CreatedAt, true) => query
                .OrderByDescending(patient => patient.CreatedAt)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.CreatedAt, false) => query
                .OrderBy(patient => patient.CreatedAt)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id),
            (PatientSortBy.UpdatedAt, true) => query
                .OrderByDescending(patient => patient.UpdatedAt.HasValue)
                .ThenByDescending(patient => patient.UpdatedAt)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id),
            (PatientSortBy.UpdatedAt, false) => query
                .OrderByDescending(patient => patient.UpdatedAt.HasValue)
                .ThenBy(patient => patient.UpdatedAt)
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
