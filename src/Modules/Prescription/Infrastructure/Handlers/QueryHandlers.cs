using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Queries;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

public sealed class GetPrescriptionHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetPrescriptionQuery, PrescriptionDto?>
{
    public async Task<PrescriptionDto?> Handle(GetPrescriptionQuery request, CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return null;

        string? trackingCode = await dbContext.PrescriptionSubmissions
            .AsNoTracking()
            .Where(s => s.PrescriptionRecordId == prescription.Id && s.RemoteTrackingCode != null)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => s.RemoteTrackingCode)
            .FirstOrDefaultAsync(cancellationToken);

        return new PrescriptionDto
        {
            Id = prescription.Id,
            PatientId = prescription.PatientId,
            PatientNationalId = prescription.PatientNationalId,
            DoctorId = prescription.DoctorId,
            ClinicId = prescription.ClinicId,
            SelectedProviderKey = prescription.SelectedProviderKey,
            Status = prescription.Status,
            VersionNumber = prescription.VersionNumber,
            CreatedAt = prescription.CreatedAt,
            SubmittedAt = prescription.SubmittedAt,
            TrackingCode = trackingCode,
            Items = prescription.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                Category = i.Category,
                ReferenceCode = i.ReferenceCode,
                DisplayName = i.DisplayName,
                DosageJson = i.DosageJson,
                Quantity = i.Quantity,
                DurationDays = i.DurationDays,
                SortOrder = i.SortOrder
            }).ToList()
        };
    }
}

public sealed class GetPrescriptionHistoryHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetPrescriptionHistoryQuery, IReadOnlyList<PrescriptionSummaryDto>>
{
    public async Task<IReadOnlyList<PrescriptionSummaryDto>> Handle(GetPrescriptionHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Prescriptions.AsNoTracking();

        if (request.PatientId.HasValue)
            query = query.Where(p => p.PatientId == request.PatientId.Value);

        if (request.ClinicId.HasValue)
            query = query.Where(p => p.ClinicId == request.ClinicId.Value);

        if (!request.PatientId.HasValue && !request.ClinicId.HasValue)
            return [];

        if (request.StatusFilter.HasValue)
            query = query.Where(p => p.Status == request.StatusFilter.Value);

        if (!string.IsNullOrEmpty(request.ProviderKeyFilter))
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKeyFilter);

        if (request.FromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new PrescriptionSummaryDto
            {
                Id = p.Id,
                Status = p.Status,
                SelectedProviderKey = p.SelectedProviderKey,
                ItemCount = p.Items.Count,
                CreatedAt = p.CreatedAt,
                SubmittedAt = p.SubmittedAt
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetEligibilityHandler(IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<GetEligibilityQuery, EligibilityResultDto>
{
    public async Task<EligibilityResultDto> Handle(GetEligibilityQuery request, CancellationToken cancellationToken)
    {
        var adapter = providerRegistry.GetAdapter(request.ProviderKey);
        if (adapter is null)
        {
            return new EligibilityResultDto
            {
                IsSuccess = false,
                Status = Domain.Enums.EligibilityStatus.Unknown,
                ErrorMessage = "آداپتور بیمه‌گر در دسترس نیست."
            };
        }

        var result = await adapter.CheckEntitlementAsync(new EntitlementRequest
        {
            PatientNationalId = request.PatientNationalId,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId
        }, cancellationToken);

        if (result.IsSuccess && result.Data is not null)
        {
            return new EligibilityResultDto
            {
                IsSuccess = true,
                Status = result.Data.Status,
                TrackingCode = result.Data.TrackingCode,
                CoverageIdentifier = result.Data.CoverageIdentifier,
                ExpiresAt = result.Data.ExpiresAt
            };
        }

        if (result.IsNotSupported)
        {
            return new EligibilityResultDto
            {
                IsSuccess = false,
                Status = Domain.Enums.EligibilityStatus.Unknown,
                ErrorMessage = "بیمه‌گر از استعلام حق بیمه پشتیبانی نمی‌کند."
            };
        }

        return new EligibilityResultDto
        {
            IsSuccess = false,
            Status = Domain.Enums.EligibilityStatus.ProviderUnavailable,
            ErrorMessage = result.Error?.UserMessage ?? "خطا در استعلام حق بیمه."
        };
    }
}

public sealed class SearchReferenceCodesHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<SearchReferenceCodesQuery, IReadOnlyList<ReferenceCodeDto>>
{
    public async Task<IReadOnlyList<ReferenceCodeDto>> Handle(SearchReferenceCodesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ReferenceCodes
            .AsNoTracking()
            .Where(r => r.ProviderKey == request.ProviderKey && r.CodeSystem == request.CodeSystem && r.IsActive);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(r => r.DisplayName.Contains(request.SearchTerm) || r.Code.Contains(request.SearchTerm));

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(r => r.Category == request.Category);

        return await query
            .Take(request.Take)
            .Select(r => new ReferenceCodeDto
            {
                Code = r.Code,
                DisplayName = r.DisplayName,
                Category = r.Category,
                CodeSystem = r.CodeSystem
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetAvailableProvidersHandler(IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<GetAvailableProvidersQuery, IReadOnlyList<AvailableProviderDto>>
{
    public Task<IReadOnlyList<AvailableProviderDto>> Handle(GetAvailableProvidersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AvailableProviderDto> result = providerRegistry
            .GetRegisteredProviderKeys()
            .Select(key => new AvailableProviderDto { ProviderKey = key })
            .ToList();

        return Task.FromResult(result);
    }
}
