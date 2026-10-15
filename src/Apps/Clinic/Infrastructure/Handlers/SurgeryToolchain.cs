using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Models;
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
    ISurgeryCatalogStore catalogs,
    ISurgeryTeamDirectory teamDirectory,
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    TimeProvider timeProvider,
    IOperationReceiptStore<SurgeryCreateOperation> operations,
    ISurgerySettingsStore settingsStore,
    ISurgeryPlanningAuthorization authorization,
    IEnumerable<IProjectObjectMaterializedViewRefresher> viewRefreshers,
    ILogger<SurgeryToolchain> logger) : ISurgeryToolchain
{
    private static readonly TimeSpan OperationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan OperationLease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ConflictWindow = TimeSpan.FromDays(1);
    private const int CalendarEntryLimit = 500;
    private const int SuggestionLimit = 50;
    private const string OperationScope = "clinic.patient-surgery-create";

    public async Task<IReadOnlyList<SurgeryReadModel>> GetUpcomingAsync(CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        DateTimeOffset now = timeProvider.GetUtcNow();
        return (await dbContext.SurgeryViews.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.Status == nameof(SurgeryStatus.Planned) && item.ScheduledAt >= now)
            .OrderBy(item => item.ScheduledAt)
            .ToListAsync(cancellationToken))
            .Select(ToRead)
            .ToArray();
    }

    public async Task<IReadOnlyList<SurgeryCalendarEntry>> GetCalendarAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (to <= from) throw new ArgumentException("The calendar range end must be after its start.", nameof(to));
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        return (await (from surgery in dbContext.SurgeryViews.AsNoTracking()
                       join patient in dbContext.PatientViews.AsNoTracking()
                           on new { surgery.TenantId, Id = surgery.PatientId } equals new { patient.TenantId, Id = patient.Id } into matches
                       from patient in matches.DefaultIfEmpty()
                       where surgery.TenantId == tenantId && surgery.ScheduledAt >= @from && surgery.ScheduledAt < to
                       orderby surgery.ScheduledAt, surgery.Id
                       select new { Surgery = surgery, PatientName = patient == null ? null : patient.Name })
            .Take(CalendarEntryLimit)
            .ToListAsync(cancellationToken))
            .Select(row => new SurgeryCalendarEntry(ToRead(row.Surgery), row.PatientName))
            .ToArray();
    }

    public async Task<IReadOnlyList<SurgeryReadModel>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        return (await dbContext.SurgeryViews.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.PatientId == patientId)
            .OrderByDescending(item => item.ScheduledAt)
            .ToListAsync(cancellationToken))
            .Select(ToRead)
            .ToArray();
    }

    public async Task<SurgeryCalendarEntry?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null) return null;
        if (current.ObjectType != SurgeryObjectTypes.ObjectType || current.OwnerEntityType != SurgeryObjectTypes.OwnerEntityType) return null;
        string? patientName = await dbContext.PatientViews.AsNoTracking()
            .Where(patient => patient.TenantId == tenantId && patient.Id == current.OwnerEntityId)
            .Select(patient => patient.Name)
            .FirstOrDefaultAsync(cancellationToken);
        return new SurgeryCalendarEntry(ToRead(current), patientName);
    }

    public async Task<IReadOnlyList<SurgeryConflict>> FindConflictsAsync(SurgeryConflictProbe probe, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        DateTimeOffset windowStart = probe.ScheduledAt - ConflictWindow;
        DateTimeOffset windowEnd = probe.ScheduledAt + ConflictWindow + TimeSpan.FromMinutes(probe.DurationMinutes ?? 0);
        var rows = await (from surgery in dbContext.SurgeryViews.AsNoTracking()
                          join patient in dbContext.PatientViews.AsNoTracking()
                              on new { surgery.TenantId, Id = surgery.PatientId } equals new { patient.TenantId, Id = patient.Id } into matches
                          from patient in matches.DefaultIfEmpty()
                          where surgery.TenantId == tenantId
                              && surgery.Status == nameof(SurgeryStatus.Planned)
                              && surgery.ScheduledAt >= windowStart
                              && surgery.ScheduledAt <= windowEnd
                          orderby surgery.ScheduledAt
                          select new { Surgery = surgery, PatientName = patient == null ? null : patient.Name })
            .ToListAsync(cancellationToken);

        List<SurgeryConflict> conflicts = [];
        foreach (var row in rows)
        {
            SurgeryReadModel entry = ToRead(row.Surgery);
            if (probe.ExcludeSurgeryId == entry.Id) continue;
            if (!SurgeryPlanRules.Overlaps(probe.ScheduledAt, probe.DurationMinutes, entry.ScheduledAt, entry.DurationMinutes)) continue;

            List<SurgeryConflictReason> reasons = [];
            if (SurgeryPlanRules.SameLocation(probe.Location, entry.Location)) reasons.Add(SurgeryConflictReason.SameLocation);

            string[] sharedTeamUserIds = probe.TeamUserIds
                .Where(userId => entry.Team.Any(member => string.Equals(member.UserId, userId, StringComparison.Ordinal)))
                .ToArray();
            if (sharedTeamUserIds.Length > 0) reasons.Add(SurgeryConflictReason.SharedTeamMember);

            if (reasons.Count == 0) continue;
            conflicts.Add(new SurgeryConflict(new SurgeryCalendarEntry(entry, row.PatientName), reasons, sharedTeamUserIds));
        }

        return conflicts;
    }

    public async Task<SurgerySuggestions> GetSuggestionsAsync(CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        IReadOnlyList<SurgerySuggestionView> suggestions = await dbContext.SurgerySuggestionViews.AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.UseCount)
            .ThenByDescending(item => item.LastUsedAt)
            .ThenBy(item => item.Value)
            .ToListAsync(cancellationToken);

        string[] locations = suggestions
            .Where(item => item.Kind == SurgerySuggestionKinds.Location)
            .Take(SuggestionLimit)
            .Select(item => item.Value)
            .ToArray();
        string[] roles = suggestions
            .Where(item => item.Kind == SurgerySuggestionKinds.Role)
            .Take(SuggestionLimit)
            .Select(item => item.Value)
            .ToArray();

        return new SurgerySuggestions(locations, roles);
    }

    public async Task<SurgeryReadModel> CreateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        if (await patients.FindByIdAsync(surgery.PatientId, cancellationToken) is null)
        {
            throw new ArgumentException("The patient does not exist.", nameof(surgery.PatientId));
        }

        if (surgery.Status != SurgeryStatus.Planned)
        {
            throw new ArgumentException("A new surgery must be planned.", nameof(surgery.Status));
        }

        surgery = surgery with { IdempotencyKey = NormalizeIdempotencyKey(surgery.IdempotencyKey) };
        surgery = await PrepareAsync(surgery, null, cancellationToken);
        JsonElement objectData = JsonSerializer.SerializeToElement(
            surgery with { Id = Guid.Empty, ConcurrencyToken = Guid.Empty },
            SurgeryJsonSerializerContext.Default.SurgeryWriteModel);
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");
        OperationReceiptKey receiptKey = new(OperationScope, tenantId, surgery.IdempotencyKey!);
        string fingerprint = Fingerprint(objectData);
        SurgeryCreateOperation claimed = new(fingerprint, Guid.CreateVersion7(), SurgeryCreateOperationState.Creating, timeProvider.GetUtcNow().UtcDateTime.Add(OperationLease));
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
            if (operation.LeaseExpiresAt > timeProvider.GetUtcNow().UtcDateTime)
            {
                throw new InvalidOperationException("The surgery operation is already in progress.");
            }

            SurgeryCreateOperation takeover = operation with { LeaseExpiresAt = timeProvider.GetUtcNow().UtcDateTime.Add(OperationLease) };
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

        SurgeryCreateOperation completedOperation = operation with { State = SurgeryCreateOperationState.Completed, LeaseExpiresAt = timeProvider.GetUtcNow().UtcDateTime.Add(OperationLifetime) };
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
        await RefreshSurgeryViewAsync(cancellationToken);
        return result;
    }

    public async Task<SurgeryReadModel?> UpdateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        ProjectObjectReadDto? current = await projectObjects.GetAsync(surgery.Id, cancellationToken);
        if (current is null || current.DeletedAt is not null) return null;
        if (current.ObjectType != SurgeryObjectTypes.ObjectType || current.OwnerEntityType != SurgeryObjectTypes.OwnerEntityType)
        {
            return null;
        }
        SurgeryReadModel currentSurgery = ToRead(current);
        SurgeryPlanRules.EnsureEditable(currentSurgery);
        SurgeryPlanRules.EnsureEditedStatus(surgery.Status, surgery.ScheduledAt, timeProvider.GetUtcNow());
        surgery = surgery with
        {
            PatientId = current.OwnerEntityId,
            IdempotencyKey = ReadIdempotencyKey(current.Data)
        };
        surgery = await PrepareAsync(surgery, currentSurgery.CatalogId, cancellationToken);
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
        await RefreshSurgeryViewAsync(cancellationToken);
        return result;
    }

    public async Task<SurgeryReadModel?> CompleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
        ProjectObjectReadDto? current = await projectObjects.GetAsync(id, cancellationToken);
        if (current is null || current.DeletedAt is not null) return null;
        if (current.ObjectType != SurgeryObjectTypes.ObjectType || current.OwnerEntityType != SurgeryObjectTypes.OwnerEntityType)
        {
            return null;
        }
        SurgeryReadModel currentSurgery = ToRead(current);
        SurgeryPlanRules.EnsureCompletable(currentSurgery, timeProvider.GetUtcNow());
        SurgeryWriteModel surgery = ToWrite(currentSurgery) with { Status = SurgeryStatus.Completed };
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
        await RefreshSurgeryViewAsync(cancellationToken);
        return result;
    }

    public async Task<SurgeryReadModel?> CancelAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken)
    {
        EnsureCanPlan();
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
        if (!SurgeryPlanRules.IsCancellable(currentSurgery.Status))
        {
            throw new InvalidOperationException("The surgery cannot be cancelled.");
        }
        SurgeryWriteModel surgery = ToWrite(currentSurgery) with { Status = SurgeryStatus.Cancelled };
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
        await RefreshSurgeryViewAsync(cancellationToken);
        return result;
    }

    private void EnsureCanPlan()
    {
        if (!authorization.CanPlan()) throw new SurgeryPlanningForbiddenException();
    }

    private async Task<SurgeryWriteModel> PrepareAsync(SurgeryWriteModel surgery, string? previousCatalogId, CancellationToken cancellationToken)
    {
        surgery = surgery with { Location = SurgeryPlanRules.NormalizeText(surgery.Location) };
        IReadOnlyList<SurgeryTeamMember> providedTeam = surgery.Team ?? [];
        IReadOnlyList<SurgeryTeamMember> team = providedTeam
            .Select(member => member with { UserId = member.UserId.Trim(), Role = member.Role.Trim() })
            .ToArray();

        if (previousCatalogId is null)
        {
            SurgeryCatalog catalog = await catalogs.FindAsync(surgery.CatalogId, cancellationToken)
                ?? throw new ArgumentException("The surgery type does not exist.", nameof(surgery.CatalogId));
            surgery = surgery with
            {
                Tools = surgery.Tools.Count == 0 ? catalog.Tools : surgery.Tools,
                ReminderPolicies = surgery.ReminderPolicies.Count == 0 ? catalog.ReminderPolicies : surgery.ReminderPolicies
            };
        }
        else if (!string.Equals(previousCatalogId, surgery.CatalogId, StringComparison.Ordinal))
        {
            _ = await catalogs.FindAsync(surgery.CatalogId, cancellationToken)
                ?? throw new ArgumentException("The surgery type does not exist.", nameof(surgery.CatalogId));
        }

        if (team.Count > 0)
        {
            IReadOnlyList<SurgeryTeamContact> members = await teamDirectory.FindMembersAsync(
                team.Select(member => member.UserId).ToArray(),
                cancellationToken);
            Dictionary<string, SurgeryTeamContact> membersById = members.ToDictionary(member => member.UserId, StringComparer.Ordinal);
            SurgeryTeamMember[] resolved = new SurgeryTeamMember[team.Count];
            for (int index = 0; index < team.Count; index++)
            {
                if (!membersById.TryGetValue(team[index].UserId, out SurgeryTeamContact member))
                {
                    throw new ArgumentException("Each team member must belong to this clinic.", nameof(surgery.Team));
                }

                resolved[index] = team[index] with { DisplayName = member.DisplayName };
            }

            team = resolved;
            surgery = surgery with { Team = team, PersonsToNotify = SurgeryPlanRules.TeamRoles(team) };
        }
        else
        {
            surgery = surgery with { Team = team };
        }

        SurgeryPlanRules.ValidatePlan(surgery);
        return surgery;
    }

    /// <summary>
    /// Refreshes the surgery view before a write returns, so the calendar, lists and the double-booking check
    /// see it at once. The background refresh the write queued still runs and catches up if this one fails.
    /// </summary>
    private async Task RefreshSurgeryViewAsync(CancellationToken cancellationToken)
    {
        IProjectObjectMaterializedViewRefresher? refresher = viewRefreshers.FirstOrDefault(
            item => string.Equals(item.ObjectType, SurgeryObjectTypes.ObjectType, StringComparison.Ordinal));
        if (refresher is null) return;
        try
        {
            await refresher.RefreshAsync(requestContext.TenantId ?? string.Empty, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The surgery view could not be refreshed inline; the queued background refresh will update it.");
        }
    }

    private async Task RebuildRemindersAsync(SurgeryReadModel surgery, CancellationToken cancellationToken)
    {
        if (surgery.Status == SurgeryStatus.Completed)
        {
            await reminders.ReplaceBySourceAsync(surgery.Id, [], cancellationToken);
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<SurgeryContact> recipients = [.. surgery.Contacts];

        PatientView? patient = await patients.FindByIdAsync(surgery.PatientId, cancellationToken);
        if (patient is not null)
        {
            if (!string.IsNullOrWhiteSpace(patient.Phone)) recipients.Add(new SurgeryContact(patient.Name, patient.Phone, SurgeryChannels.Sms));
            if (!string.IsNullOrWhiteSpace(patient.Email)) recipients.Add(new SurgeryContact(patient.Name, patient.Email, SurgeryChannels.Email));
        }

        if (surgery.Team.Count > 0)
        {
            IReadOnlyList<SurgeryTeamContact> members = await teamDirectory.FindMembersAsync(
                surgery.Team.Select(member => member.UserId).ToArray(),
                cancellationToken);
            foreach (SurgeryTeamContact member in members)
            {
                if (!string.IsNullOrWhiteSpace(member.Email)) recipients.Add(new SurgeryContact(member.DisplayName, member.Email, SurgeryChannels.Email));
                if (!string.IsNullOrWhiteSpace(member.PhoneNumber)) recipients.Add(new SurgeryContact(member.DisplayName, member.PhoneNumber, SurgeryChannels.Sms));
            }
        }

        // Cancelling sends the clinic's own cancellation message. Clinics get settings when an owner first opens
        // surgery planning; the built-in English text only covers surgeries cancelled before that.
        IReadOnlyList<SurgeryReminderPolicy> policies = surgery.ReminderPolicies;
        if (surgery.Status == SurgeryStatus.Cancelled)
        {
            SurgerySettings? clinicSettings = await settingsStore.GetAsync(cancellationToken);
            policies = clinicSettings is null
                ? [new SurgeryReminderPolicy("Cancellation", 0, "Surgery cancelled", "The scheduled surgery has been cancelled.")]
                : [new SurgeryReminderPolicy("Cancellation", 0, clinicSettings.CancellationSubject, clinicSettings.CancellationTextBody)];
        }

        List<ReminderWriteDto> replacements = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (SurgeryContact contact in recipients)
        {
            if (!SurgeryChannels.IsSupported(contact.Channel) || !Enum.TryParse(contact.Channel, true, out ReminderKind kind)) continue;
            if (!seen.Add($"{contact.Channel}\u0000{contact.Destination}")) continue;
            foreach (SurgeryReminderPolicy policy in policies)
            {
                DateTime remindAt = surgery.Status == SurgeryStatus.Cancelled
                    ? now.UtcDateTime
                    : surgery.ScheduledAt.UtcDateTime.AddMinutes(-policy.MinutesBefore);
                if (surgery.Status != SurgeryStatus.Cancelled && remindAt <= now.UtcDateTime) continue;
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
            ? new SurgeryReadModel(value.Id, value.OwnerEntityId, surgery.CatalogId, surgery.Name, surgery.IdempotencyKey, surgery.Status, surgery.ScheduledAt, surgery.Tools, surgery.PersonsToNotify, surgery.Contacts, surgery.ReminderPolicies, value.ConcurrencyToken, value.CreatedAt, value.UpdatedAt, surgery.DurationMinutes, surgery.Location, surgery.Team ?? [])
            : throw new InvalidOperationException("The stored surgery object is invalid.");

    private static SurgeryReadModel ToRead(SurgeryView value)
        => JsonSerializer.Deserialize(value.Data.RootElement, SurgeryJsonSerializerContext.Default.SurgeryWriteModel) is { } surgery
            ? new SurgeryReadModel(value.Id, value.PatientId, surgery.CatalogId, surgery.Name, surgery.IdempotencyKey, surgery.Status, surgery.ScheduledAt, surgery.Tools, surgery.PersonsToNotify, surgery.Contacts, surgery.ReminderPolicies, value.ConcurrencyToken, value.CreatedAt, value.UpdatedAt, surgery.DurationMinutes, surgery.Location, surgery.Team ?? [])
            : throw new InvalidOperationException("The surgery materialized view contains invalid data.");

    private static SurgeryWriteModel ToWrite(SurgeryReadModel value)
        => new(
            value.Id,
            value.PatientId,
            value.CatalogId,
            value.Name,
            value.IdempotencyKey,
            value.Status,
            value.ScheduledAt,
            value.Tools,
            value.PersonsToNotify,
            value.Contacts,
            value.ReminderPolicies,
            value.ConcurrencyToken,
            value.DurationMinutes,
            value.Location,
            value.Team);

    private static string? NormalizeIdempotencyKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Hashes the surgery the data describes rather than its JSON text: the project-object store re-serializes
    /// stored data, so the same surgery can come back with different escaping or formatting.
    /// </summary>
    private static string Fingerprint(JsonElement data)
    {
        SurgeryWriteModel surgery = JsonSerializer.Deserialize(data, SurgeryJsonSerializerContext.Default.SurgeryWriteModel)
            ?? throw new InvalidOperationException("The surgery data is invalid.");
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(
            surgery with { Id = Guid.Empty, ConcurrencyToken = Guid.Empty },
            SurgeryJsonSerializerContext.Default.SurgeryWriteModel);
        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }

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
