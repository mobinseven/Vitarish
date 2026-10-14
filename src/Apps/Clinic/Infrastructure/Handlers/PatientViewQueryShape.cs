using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Entities.Server.Infrastructure.Search;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class PatientViewQueryShape(EntitySearchPatternFactory searchPatternFactory)
{
    public IQueryable<PatientView> CreateQuery(ClinicDbContext dbContext, IReadOnlyList<string>? tags)
    {
        string[] normalizedTags = PatientTags.Normalize(tags);
        if (normalizedTags.Length == 0)
        {
            return dbContext.PatientViews.AsNoTracking();
        }

        NpgsqlParameter<string[]> tagParameter = new("tags", normalizedTags)
        {
            NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
        };

        return dbContext.PatientViews
            .FromSqlRaw("SELECT * FROM \"Clinic\".\"mv_Patients\" WHERE \"Tags\" @> @tags", tagParameter)
            .AsNoTracking();
    }

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
            IReadOnlyCollection<string> fragments = searchPatternFactory.CreateSearchFragments(search);
            foreach (string fragment in fragments)
            {
                query = query.Where(patient =>
                    patient.Name.ToLower().Contains(fragment)
                    || (patient.Description != null && patient.Description.ToLower().Contains(fragment))
                    || (patient.IdentificationCode != null && patient.IdentificationCode.ToLower().Contains(fragment))
                    || (patient.FirstName != null && patient.FirstName.ToLower().Contains(fragment))
                    || (patient.LastName != null && patient.LastName.ToLower().Contains(fragment))
                    || (patient.Email != null && patient.Email.ToLower().Contains(fragment))
                    || (patient.Phone != null && patient.Phone.ToLower().Contains(fragment)));
            }
        }

        return query;
    }

    public IQueryable<PatientView> ApplySort(
        IQueryable<PatientView> query,
        PatientSortBy? sortBy,
        bool descending,
        ClinicDbContext dbContext,
        DateTimeOffset now)
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
            (PatientSortBy.LastSurgery, _) => OrderBySurgeryDate(query, PatientSurgeryDates.LastKey(dbContext, now), descending),
            (PatientSortBy.NextSurgery, _) => OrderBySurgeryDate(query, PatientSurgeryDates.NextKey(dbContext, now), descending),
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

    /// <summary>Patients with a date first, then by the date, then by name; matches the other date sorts.</summary>
    private static IQueryable<PatientView> OrderBySurgeryDate(
        IQueryable<PatientView> query,
        Expression<Func<PatientView, DateTimeOffset?>> key,
        bool descending)
    {
        Expression<Func<PatientView, bool>> hasKey = Expression.Lambda<Func<PatientView, bool>>(
            Expression.Property(key.Body, nameof(Nullable<DateTimeOffset>.HasValue)),
            key.Parameters);
        IOrderedQueryable<PatientView> ordered = query.OrderByDescending(hasKey);
        return descending
            ? ordered
                .ThenByDescending(key)
                .ThenByDescending(patient => patient.LastName ?? patient.Name)
                .ThenByDescending(patient => patient.FirstName)
                .ThenByDescending(patient => patient.Id)
            : ordered
                .ThenBy(key)
                .ThenBy(patient => patient.LastName ?? patient.Name)
                .ThenBy(patient => patient.FirstName)
                .ThenBy(patient => patient.Id);
    }
}
