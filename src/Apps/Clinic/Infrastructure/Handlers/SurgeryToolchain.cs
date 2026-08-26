using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Serializers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class SurgeryToolchain(
    IProjectObjectService projectObjects,
    IRemindersService reminders,
    IPatientDirectory patients,
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    IOperationReceiptStore<SurgeryCreateOperation> operations) : ISurgeryToolchain
{
    private static readonly TimeSpan OperationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan OperationLease = TimeSpan.FromMinutes(2);
    private const string OperationScope = "clinic.patient-surgery-create";
    public IReadOnlyList<SurgeryCatalog> GetCatalogs() => SurgeryCatalogs.All;

    public async Task<IReadOnlyList<SurgeryReadModel>> GetUpcomingAsync(CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return (await dbContext.SurgeryViews.AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken))
            .Select(ToRead)
            .Where(item => item.Status == SurgeryStatus.Planned && item.ScheduledAt >= now)
            .OrderBy(item => item.ScheduledAt)
            .ToArray();
    }

    public async Task<IReadOnlyList<SurgeryReadModel>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        return (await dbContext.SurgeryViews.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.PatientId == patientId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken))
            .Select(ToRead)
            .ToArray();
    }

    public async Task<SurgeryReadModel?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        SurgeryView? value = await dbContext.SurgeryViews.AsNoTracking()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Id == id, cancellationToken);
        return value is null ? null : ToRead(value);
    }

    public async Task<SurgeryReadModel> CreateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken)
    {
        if (await patients.FindByIdAsync(surgery.PatientId, cancellationToken) is null)
        {
            throw new ArgumentException("The patient does not exist.", nameof(surgery.PatientId));
        }

        SurgeryCatalog catalog = SurgeryCatalogs.Find(surgery.CatalogId)
            ?? throw new ArgumentException("The surgery catalog does not exist.", nameof(surgery.CatalogId));
        surgery = surgery with
        {
            IdempotencyKey = NormalizeIdempotencyKey(surgery.IdempotencyKey),
            Tools = surgery.Tools.Count == 0 ? catalog.Tools : surgery.Tools,
            PersonsToNotify = surgery.PersonsToNotify.Count == 0 ? catalog.PersonsToNotify : surgery.PersonsToNotify,
            ReminderPolicies = surgery.ReminderPolicies.Count == 0 ? catalog.ReminderPolicies : surgery.ReminderPolicies
        };
        Validate(surgery);
        JsonElement objectData = JsonSerializer.SerializeToElement(
            surgery with { Id = Guid.Empty, ConcurrencyToken = Guid.Empty },
            SurgeryJsonSerializerContext.Default.SurgeryWriteModel);
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        OperationReceiptKey receiptKey = new(OperationScope, tenantId, surgery.IdempotencyKey!);
        string fingerprint = Fingerprint(objectData);
        SurgeryCreateOperation claimed = new(fingerprint, Guid.CreateVersion7(), SurgeryCreateOperationState.Creating, DateTime.UtcNow.Add(OperationLease));
        OperationReceipt<SurgeryCreateOperation> receipt;
        bool ownsLease = false;
        try
        {
            OperationReceipt<SurgeryCreateOperation>? existingReceipt = await operations.GetAsync(receiptKey, cancellationToken);
            if (existingReceipt is null)
            {
                if (await operations.TryClaimAsync(receiptKey, claimed, OperationLifetime, cancellationToken))
                {
                    receipt = new(0, claimed);
                    ownsLease = true;
                }
                else
                {
                    receipt = await operations.GetAsync(receiptKey, cancellationToken)
                        ?? throw new SurgeryOperationUnavailableException(new InvalidOperationException("The surgery operation receipt could not be claimed."));
                }
            }
            else
            {
                receipt = existingReceipt;
            }
        }
        catch (SurgeryOperationUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new SurgeryOperationUnavailableException(exception);
        }

        if (!string.Equals(receipt.Value.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new ArgumentException("Idempotency key is already used for another surgery request.");
        }

        if (receipt.Value.State == SurgeryCreateOperationState.Completed)
        {
            ProjectObjectReadDto? completed = await projectObjects.GetAsync(receipt.Value.SurgeryId, cancellationToken);
            return completed is null
                ? throw new SurgeryOperationUnavailableException(new InvalidOperationException("The completed surgery could not be retrieved."))
                : ToRead(completed);
        }

        SurgeryCreateOperation operation = receipt.Value;
        if (!ownsLease)
        {
            if (operation.LeaseExpiresAt > DateTime.UtcNow)
            {
                throw new InvalidOperationException("The surgery operation is already in progress.");
            }

            SurgeryCreateOperation takeover = operation with { LeaseExpiresAt = DateTime.UtcNow.Add(OperationLease) };
            if (!await operations.TryUpdateAsync(receiptKey, receipt.Revision, takeover, OperationLifetime, cancellationToken))
            {
                throw new InvalidOperationException("The surgery operation is already in progress.");
            }

            operation = takeover;
            receipt = new(receipt.Revision + 1, operation);
        }

        ProjectObjectReadDto? value = await projectObjects.GetAsync(operation.SurgeryId, cancellationToken);
        bool createdHere = false;
        try
        {
            if (value is null)
            {
                value = await projectObjects.CreateAsync(
                    SurgeryObjectTypes.OwnerEntityType,
                    surgery.PatientId,
                    SurgeryObjectTypes.ObjectType,
                    objectData,
                    cancellationToken,
                    operation.SurgeryId);
                createdHere = true;
            }
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            await CompensateDeleteAsync(exception.ObjectId, exception.ConcurrencyToken, exception);
        }
        if (value is null) throw new SurgeryOperationUnavailableException(new InvalidOperationException("The surgery object could not be created."));
        SurgeryReadModel result = ToRead(value);
        if (!string.Equals(Fingerprint(value.Data), fingerprint, StringComparison.Ordinal))
        {
            throw new ArgumentException("Idempotency key is already used for another surgery request.");
        }
        try
        {
            await RebuildRemindersAsync(result, cancellationToken);
        }
        catch (Exception exception)
        {
            if (createdHere)
            {
                await CompensateDeleteAsync(value, exception);
            }

            throw new SurgeryOperationUnavailableException(exception);
        }

        SurgeryCreateOperation completedOperation = operation with { State = SurgeryCreateOperationState.Completed, LeaseExpiresAt = DateTime.UtcNow.Add(OperationLifetime) };
        try
        {
            if (!await operations.TryUpdateAsync(receiptKey, receipt.Revision, completedOperation, OperationLifetime, cancellationToken))
            {
                throw new InvalidOperationException("The surgery operation receipt could not be completed.");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new SurgeryOperationUnavailableException(exception);
        }
        return result;
    }

    public async Task<SurgeryReadModel?> UpdateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? current = await projectObjects.GetAsync(surgery.Id, cancellationToken);
        if (current is null || current.DeletedAt is not null) return null;
        if (current.ObjectType != SurgeryObjectTypes.ObjectType || current.OwnerEntityType != SurgeryObjectTypes.OwnerEntityType)
        {
            return null;
        }
        surgery = surgery with
        {
            PatientId = current.OwnerEntityId,
            IdempotencyKey = ReadIdempotencyKey(current.Data)
        };
        Validate(surgery);
        ProjectObjectReadDto? value;
        try
        {
            value = await projectObjects.UpdateAsync(
                surgery.Id,
                SurgeryObjectTypes.OwnerEntityType,
                surgery.PatientId,
                SurgeryObjectTypes.ObjectType,
                JsonSerializer.SerializeToElement(
                    surgery with { Id = Guid.Empty, ConcurrencyToken = Guid.Empty },
                    SurgeryJsonSerializerContext.Default.SurgeryWriteModel),
                surgery.ConcurrencyToken,
                cancellationToken);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            await CompensateRestoreAsync(current, exception.ConcurrencyToken, exception);
            throw new SurgeryOperationUnavailableException(exception);
        }
        if (value is null) return null;
        SurgeryReadModel result = ToRead(value);
        try
        {
            await RebuildRemindersAsync(result, cancellationToken);
        }
        catch (Exception exception)
        {
            await CompensateRestoreAsync(current, value.ConcurrencyToken, exception);
        }
        return result;
    }

    public async Task<SurgeryReadModel?> CancelAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null) return null;
        if (current.ObjectType != SurgeryObjectTypes.ObjectType || current.OwnerEntityType != SurgeryObjectTypes.OwnerEntityType)
        {
            return null;
        }
        SurgeryReadModel currentSurgery = ToRead(current);
        if (currentSurgery.Status == SurgeryStatus.Cancelled)
        {
            return currentSurgery;
        }
        SurgeryReadModel surgery = currentSurgery with { Status = SurgeryStatus.Cancelled };
        ProjectObjectReadDto? value;
        try
        {
            value = await projectObjects.UpdateAsync(
                id,
                SurgeryObjectTypes.OwnerEntityType,
                surgery.PatientId,
                SurgeryObjectTypes.ObjectType,
                JsonSerializer.SerializeToElement(
                    surgery with { Id = Guid.Empty, ConcurrencyToken = Guid.Empty },
                    SurgeryJsonSerializerContext.Default.SurgeryWriteModel),
                concurrencyToken,
                cancellationToken);
        }
        catch (ProjectObjectRefreshUnavailableException exception)
        {
            await CompensateRestoreAsync(current, exception.ConcurrencyToken, exception);
            throw new SurgeryOperationUnavailableException(exception);
        }
        if (value is null) return null;
        SurgeryReadModel result = ToRead(value);
        try
        {
            await RebuildRemindersAsync(result, cancellationToken);
        }
        catch (Exception exception)
        {
            await CompensateRestoreAsync(current, value.ConcurrencyToken, exception);
        }
        return result;
    }

    private async Task RebuildRemindersAsync(SurgeryReadModel surgery, CancellationToken cancellationToken)
    {
        List<ReminderWriteDto> replacements = [];

        foreach (SurgeryContact contact in surgery.Contacts)
        {
            if (!SurgeryChannels.IsSupported(contact.Channel) || !Enum.TryParse(contact.Channel, true, out ReminderKind kind)) continue;
            IReadOnlyList<SurgeryReminderPolicy> policies = surgery.Status == SurgeryStatus.Cancelled
                ? [new SurgeryReminderPolicy("Cancellation", 0, "Surgery cancelled", "The scheduled surgery has been cancelled.")]
                : surgery.ReminderPolicies;
            foreach (SurgeryReminderPolicy policy in policies)
            {
                DateTime remindAt = surgery.Status == SurgeryStatus.Cancelled
                    ? DateTime.UtcNow
                    : surgery.ScheduledAt.UtcDateTime.AddMinutes(-policy.MinutesBefore);
                if (surgery.Status != SurgeryStatus.Cancelled && remindAt <= DateTime.UtcNow) continue;
                replacements.Add(new ReminderWriteDto(
                    surgery.Id,
                    kind,
                    null,
                    contact.Destination,
                    policy.Subject,
                    policy.TextBody,
                    remindAt,
                    null,
                    false,
                    SurgeryObjectTypes.ObjectType,
                    policy.Name,
                    JsonSerializer.Serialize(
                        new SurgeryReminderTemplateVariables(surgery.Id, contact.Name),
                        SurgeryJsonSerializerContext.Default.SurgeryReminderTemplateVariables)));
            }
        }

        await reminders.ReplaceBySourceAsync(surgery.Id, replacements, cancellationToken);
    }

    private static SurgeryReadModel ToRead(ProjectObjectReadDto value)
        => JsonSerializer.Deserialize(value.Data, SurgeryJsonSerializerContext.Default.SurgeryWriteModel) is { } surgery
            ? new SurgeryReadModel(value.Id, value.OwnerEntityId, surgery.CatalogId, surgery.Name, surgery.IdempotencyKey, surgery.Status, surgery.ScheduledAt, surgery.Tools, surgery.PersonsToNotify, surgery.Contacts, surgery.ReminderPolicies, value.ConcurrencyToken, value.CreatedAt, value.UpdatedAt)
            : throw new InvalidOperationException("The stored surgery object is invalid.");

    private static SurgeryReadModel ToRead(SurgeryView value)
        => JsonSerializer.Deserialize(value.Data.RootElement, SurgeryJsonSerializerContext.Default.SurgeryWriteModel) is { } surgery
            ? new SurgeryReadModel(value.Id, value.PatientId, surgery.CatalogId, surgery.Name, surgery.IdempotencyKey, surgery.Status, surgery.ScheduledAt, surgery.Tools, surgery.PersonsToNotify, surgery.Contacts, surgery.ReminderPolicies, value.ConcurrencyToken, value.CreatedAt, value.UpdatedAt)
            : throw new InvalidOperationException("The surgery materialized view contains invalid data.");

    private static void Validate(SurgeryWriteModel surgery)
    {
        if (surgery.PatientId == Guid.Empty) throw new ArgumentException("A patient is required.");
        if (SurgeryCatalogs.Find(surgery.CatalogId) is null) throw new ArgumentException("The surgery catalog does not exist.");
        if (string.IsNullOrWhiteSpace(surgery.Name)) throw new ArgumentException("A surgery name is required.");
        if (string.IsNullOrWhiteSpace(surgery.IdempotencyKey)) throw new ArgumentException("An idempotency key is required.");
        if (surgery.IdempotencyKey is { Length: > 128 }) throw new ArgumentException("The idempotency key cannot exceed 128 characters.");
        if (surgery.ScheduledAt == default) throw new ArgumentException("A surgery date and time are required.");
        if (surgery.ReminderPolicies.Any(policy => policy.MinutesBefore < 0)) throw new ArgumentException("Reminder offsets cannot be negative.");
        foreach (SurgeryContact contact in surgery.Contacts)
        {
            if (string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Destination) || !SurgeryChannels.IsSupported(contact.Channel))
                throw new ArgumentException("Each surgery contact needs a name, destination, and email or sms channel.");
        }
    }

    private static string? NormalizeIdempotencyKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Fingerprint(JsonElement data)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(data.GetRawText())));

    private static string? ReadIdempotencyKey(JsonElement data)
        => (data.TryGetProperty("IdempotencyKey", out JsonElement value) || data.TryGetProperty("idempotencyKey", out value)) && value.ValueKind == JsonValueKind.String
            ? NormalizeIdempotencyKey(value.GetString())
            : null;

    private async Task CompensateDeleteAsync(ProjectObjectReadDto value, Exception originalException)
        => await CompensateDeleteAsync(value.Id, value.ConcurrencyToken, originalException);

    private async Task CompensateDeleteAsync(Guid id, Guid concurrencyToken, Exception originalException)
    {
        try
        {
            _ = await projectObjects.DeleteAsync(id, concurrencyToken, CancellationToken.None);
        }
        catch (Exception compensationException)
        {
            throw new SurgeryOperationUnavailableException(new AggregateException(originalException, compensationException));
        }

        throw new SurgeryOperationUnavailableException(originalException);
    }

    private async Task CompensateRestoreAsync(ProjectObjectReadDto original, Guid updatedConcurrencyToken, Exception originalException)
    {
        try
        {
            _ = await projectObjects.UpdateAsync(
                original.Id,
                original.OwnerEntityType,
                original.OwnerEntityId,
                original.ObjectType,
                original.Data,
                updatedConcurrencyToken,
                CancellationToken.None);
        }
        catch (Exception compensationException)
        {
            throw new SurgeryOperationUnavailableException(new AggregateException(originalException, compensationException));
        }

        throw new SurgeryOperationUnavailableException(originalException);
    }
}
