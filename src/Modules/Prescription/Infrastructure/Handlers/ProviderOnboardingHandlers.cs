using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

public sealed class RegisterProviderHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<RegisterProviderHandler> logger)
    : IRequestHandler<RegisterProviderCommand, RegisterProviderResult>
{
    public async Task<RegisterProviderResult> Handle(RegisterProviderCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.InsuranceProviders
            .AnyAsync(p => p.ProviderKey == request.ProviderKey, cancellationToken);

        if (exists)
            return RegisterProviderResult.Failure("کلید بیمه‌گر تکراری است.");

        var provider = new InsuranceProvider
        {
            Id = Guid.NewGuid(),
            ProviderKey = request.ProviderKey,
            Name = request.Name,
            ProviderType = request.ProviderType,
            IntegrationMode = request.IntegrationMode,
            Status = ProviderStatus.Disabled,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.InsuranceProviders.Add(provider);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(InsuranceProvider),
            EntityId = provider.Id,
            Action = AuditAction.Created,
            NewState = ProviderStatus.Disabled.ToString(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        logger.LogInformation("Provider {ProviderKey} registered with ID {ProviderId}", request.ProviderKey, provider.Id);
        return RegisterProviderResult.Success(provider.Id);
    }
}

public sealed class UpdateProviderHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter)
    : IRequestHandler<UpdateProviderCommand, UpdateProviderResult>
{
    public async Task<UpdateProviderResult> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await dbContext.InsuranceProviders
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken);

        if (provider is null)
            return UpdateProviderResult.Failure("بیمه‌گر یافت نشد.");

        provider.Name = request.Name;
        provider.ProviderType = request.ProviderType;
        provider.IntegrationMode = request.IntegrationMode;
        provider.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(InsuranceProvider),
            EntityId = provider.Id,
            Action = "Updated",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return UpdateProviderResult.Success();
    }
}

public sealed class SetProviderCapabilitiesHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter)
    : IRequestHandler<SetProviderCapabilitiesCommand, SetProviderCapabilitiesResult>
{
    public async Task<SetProviderCapabilitiesResult> Handle(SetProviderCapabilitiesCommand request, CancellationToken cancellationToken)
    {
        var provider = await dbContext.InsuranceProviders
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken);

        if (provider is null)
            return SetProviderCapabilitiesResult.Failure("بیمه‌گر یافت نشد.");

        // Remove existing capabilities
        var existing = await dbContext.InsuranceProviderCapabilities
            .Where(c => c.InsuranceProviderId == request.ProviderId)
            .ToListAsync(cancellationToken);

        dbContext.InsuranceProviderCapabilities.RemoveRange(existing);

        // Add new capabilities
        var newCapabilities = request.Capabilities.Select(cap => new InsuranceProviderCapability
        {
            Id = Guid.NewGuid(),
            InsuranceProviderId = request.ProviderId,
            Capability = cap,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.InsuranceProviderCapabilities.AddRange(newCapabilities);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(InsuranceProviderCapability),
            EntityId = request.ProviderId,
            Action = "CapabilitiesUpdated",
            NewState = $"Count={request.Capabilities.Count}",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return SetProviderCapabilitiesResult.Success();
    }
}

public sealed class SetProviderStatusHandler(
    PrescriptionDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<SetProviderStatusHandler> logger)
    : IRequestHandler<SetProviderStatusCommand, SetProviderStatusResult>
{
    public async Task<SetProviderStatusResult> Handle(SetProviderStatusCommand request, CancellationToken cancellationToken)
    {
        var provider = await dbContext.InsuranceProviders
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken);

        if (provider is null)
            return SetProviderStatusResult.Failure("بیمه‌گر یافت نشد.");

        var oldStatus = provider.Status;
        provider.Status = request.NewStatus;
        provider.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(InsuranceProvider),
            EntityId = provider.Id,
            Action = "StatusChanged",
            OldState = oldStatus.ToString(),
            NewState = request.NewStatus.ToString(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        logger.LogInformation("Provider {ProviderKey} status changed from {Old} to {New}",
            provider.ProviderKey, oldStatus, request.NewStatus);
        return SetProviderStatusResult.Success();
    }
}

public sealed class TestProviderConnectivityHandler(
    IInsuranceProviderRegistry providerRegistry,
    ILogger<TestProviderConnectivityHandler> logger)
    : IRequestHandler<TestProviderConnectivityCommand, TestProviderConnectivityResult>
{
    public async Task<TestProviderConnectivityResult> Handle(TestProviderConnectivityCommand request, CancellationToken cancellationToken)
    {
        var adapter = providerRegistry.GetAdapter(request.ProviderKey);
        if (adapter is null)
            return TestProviderConnectivityResult.Failure("آداپتور بیمه‌گر یافت نشد.");

        var sw = Stopwatch.StartNew();

        var authResult = await adapter.AuthenticateAsync(new AuthenticateRequest
        {
            DoctorId = Guid.Empty,
            ClinicId = Guid.Empty
        }, cancellationToken);

        sw.Stop();

        if (authResult.IsSuccess)
        {
            logger.LogInformation("Provider {ProviderKey} connectivity test passed in {Ms}ms",
                request.ProviderKey, sw.ElapsedMilliseconds);
            return TestProviderConnectivityResult.Success(sw.ElapsedMilliseconds);
        }

        if (authResult.IsNotSupported)
        {
            return TestProviderConnectivityResult.Failure("بیمه‌گر از تست اتصال پشتیبانی نمی‌کند.");
        }

        return TestProviderConnectivityResult.Failure(
            authResult.Error?.UserMessage ?? "تست اتصال ناموفق بود.");
    }
}

public sealed class GetProviderManagementGridHandler(
    PrescriptionDbContext dbContext)
    : IRequestHandler<GetProviderManagementGridQuery, IReadOnlyList<ProviderManagementDto>>
{
    public async Task<IReadOnlyList<ProviderManagementDto>> Handle(GetProviderManagementGridQuery request, CancellationToken cancellationToken)
    {
        var providers = await dbContext.InsuranceProviders
            .GroupJoin(
                dbContext.InsuranceProviderCapabilities,
                p => p.Id,
                c => c.InsuranceProviderId,
                (p, caps) => new ProviderManagementDto
                {
                    ProviderId = p.Id,
                    ProviderKey = p.ProviderKey,
                    Name = p.Name,
                    ProviderType = p.ProviderType,
                    IntegrationMode = p.IntegrationMode,
                    Status = p.Status,
                    CapabilityCount = caps.Count(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return providers;
    }
}
