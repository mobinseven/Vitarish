using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.E2e.Infrastructure.Data;

internal sealed class E2eClinicDbContext(
    DbContextOptions<ClinicDbContext> options,
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor)
    : ClinicDbContext(options, requestContext, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.Entity<PatientView>().HasKey(patient => patient.Id);
    }
}
