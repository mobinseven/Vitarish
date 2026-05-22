using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyManager(
    IShiftAggregateLoader aggregateLoader,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : IShiftBookingPolicyManager
{
    private const string PolicyEntityType = nameof(ShiftBookingPolicy);
    private const string ShiftNotFound = "ShiftBookingPolicyShiftNotFoundError";
    private const string PolicyNotFound = "ShiftBookingPolicyNotFoundError";
    private const string InvalidDate = "ShiftBookingPolicyInvalidDateError";

    public async Task<IReadOnlyList<ShiftBookingPolicyReadDto>> GetByShiftAsync(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(shiftId, cancellationToken);
        return shift is null ? [] : [.. shift.BookingPolicies.Select(p => p.ToDto())];
    }

    public async Task<EntityResult<ShiftBookingPolicyReadDto>> UpsertPolicyAsync(
        ShiftBookingPolicyWriteDto dto,
        CancellationToken cancellationToken)
    {
        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(dto.ShiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.NotFound(ShiftNotFound, nameof(dto.ShiftId)));
        }

        if (dto.Date.DayOfWeek != shift.DayOfWeek)
        {
            return EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Validation(InvalidDate, new Dictionary<string, string> { [nameof(dto.Date)] = InvalidDate }));
        }

        ShiftBookingPolicy? existing = null;

        if (dto.Id != Guid.Empty)
        {
            existing = shift.BookingPolicies.FirstOrDefault(p => p.Id == dto.Id);
        }

        existing ??= shift.BookingPolicies.FirstOrDefault(p => p.Date == dto.Date);

        if (existing is null)
        {
            existing = dto.ToDomain();
            existing.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            existing.ShiftId = dto.ShiftId;
            shift.BookingPolicies.Add(existing);
        }
        else
        {
            existing.Date = dto.Date;
            existing.BookingOpensBefore = dto.BookingOpensBefore;
            existing.BookingCloseBefore = dto.BookingCloseBefore;
            existing.Closed = dto.Closed;
            existing.Capacity = dto.Capacity;
            existing.RequiresApproval = dto.RequiresApproval;
            existing.CancellationDeadline = dto.CancellationDeadline;
            existing.EstimatedSessionLength = dto.EstimatedSessionLength;
            existing.AllowRescheduling = dto.AllowRescheduling;
            existing.MaxReservationsPerParty = dto.MaxReservationsPerParty;
            existing.ShiftId = dto.ShiftId;
        }

        bool hasDateConflict = shift.BookingPolicies
            .Any(p => p.Id != existing.Id && p.Date == dto.Date);
        if (hasDateConflict)
        {
            return EntityResult<ShiftBookingPolicyReadDto>.Failure(
                EntityError.Conflict("A policy for this date already exists."));
        }

        await aggregateLoader.SaveAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Upserted",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = existing.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        return EntityResult<ShiftBookingPolicyReadDto>.Success(existing.ToDto());
    }

    public async Task<EntityResult> RemovePolicyAsync(Guid shiftId, Guid policyId, CancellationToken cancellationToken)
    {
        Shift? shift = await aggregateLoader.LoadWithPoliciesAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult.Failure(EntityError.NotFound(ShiftNotFound, nameof(shiftId)));
        }

        ShiftBookingPolicy? existing = shift.BookingPolicies.FirstOrDefault(p => p.Id == policyId);
        if (existing is null)
        {
            return EntityResult.Failure(EntityError.NotFound(PolicyNotFound, nameof(policyId)));
        }

        shift.BookingPolicies.Remove(existing);

        await aggregateLoader.SaveAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = "Removed",
            ActorId = requestContext.UserId,
            EntityType = PolicyEntityType,
            EntityId = policyId,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        return EntityResult.Success();
    }
}
