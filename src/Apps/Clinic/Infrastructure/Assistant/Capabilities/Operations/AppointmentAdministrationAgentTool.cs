using System.Text.Json;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Models;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;

public sealed class AppointmentAdministrationAgentTool(
    IAccessControlledReservationManager reservations,
    IAccessControlledShiftManager shifts,
    IServiceManager services,
    IServiceTypeManager serviceTypes,
    IServiceProviderManager providers,
    IShiftBookingPolicyManager bookingPolicies,
    IShiftReminderPolicyManager reminderPolicies,
    IShiftBookableDateQueryService bookableDates,
    ITemporaryHoldManager holds) : IClinicOperationsAgentTool, IClinicWorkspacePreparationTool
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> Mutations = [
        "create_reservation", "update_reservation", "delete_reservation", "confirm_reservation", "present_reservation",
        "complete_reservation", "cancel_reservation", "reject_reservation", "expire_reservation", "move_reservation",
        "create_shift", "update_shift", "delete_shift", "activate_shift", "pause_shift", "close_shift", "cancel_shift", "archive_shift",
        "create_service", "update_service", "delete_service", "create_service_type", "update_service_type", "delete_service_type",
        "create_provider", "update_provider", "delete_provider", "link_provider_user", "unlink_provider_user",
        "create_booking_policy", "update_booking_policy", "delete_booking_policy",
        "create_reminder_policy", "update_reminder_policy", "delete_reminder_policy",
        "create_hold", "confirm_hold", "release_hold"];

    public AgentToolDefinition Definition { get; } = new(
        "appointment_manage",
        "Read or prepare a supported appointment-module operation for the authenticated practice. All mutations require confirmation.",
        """{"type":"object","properties":{"operation":{"type":"string","enum":["list_reservations","get_reservation","count_reservations","create_reservation","update_reservation","delete_reservation","confirm_reservation","present_reservation","complete_reservation","cancel_reservation","reject_reservation","expire_reservation","move_reservation","list_shifts","get_shift","count_shifts","create_shift","update_shift","delete_shift","activate_shift","pause_shift","close_shift","cancel_shift","archive_shift","list_services","get_service","count_services","create_service","update_service","delete_service","list_service_types","get_service_type","count_service_types","create_service_type","update_service_type","delete_service_type","list_providers","get_provider","count_providers","create_provider","update_provider","delete_provider","link_provider_user","unlink_provider_user","list_booking_policies","get_booking_policy","count_booking_policies","create_booking_policy","update_booking_policy","delete_booking_policy","list_reminder_policies","get_reminder_policy","count_reminder_policies","create_reminder_policy","update_reminder_policy","delete_reminder_policy","list_bookable_dates","list_bookable_slots","create_hold","confirm_hold","release_hold"]},"payload":{"type":"object"},"commit":{"type":"boolean"}},"required":["operation"],"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement root = document.RootElement;
        string operation = RequiredString(root, "operation");
        JsonElement payload = root.TryGetProperty("payload", out JsonElement supplied) ? supplied : default;
        bool commit = root.TryGetProperty("commit", out JsonElement commitValue) && commitValue.ValueKind == JsonValueKind.True &&
            context.Metadata is not null && context.Metadata.TryGetValue("confirmedAction", out string? confirmed) && confirmed == "true";
        if (Mutations.Contains(operation) && !commit)
        {
            Guid actionId = Guid.CreateVersion7();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            return new(call.Id, JsonSerializer.Serialize(new { status = "prepared", actionId, arguments = root.GetRawText(), expiresAt }));
        }

        object result = operation switch
        {
            "list_reservations" => await List(reservations, Read<ReservationDescriptorDto>(payload), cancellationToken),
            "get_reservation" => await Get(reservations, Id(payload), cancellationToken),
            "count_reservations" => await Count(reservations, Read<ReservationDescriptorDto>(payload), cancellationToken),
            "create_reservation" => await Add(reservations, Read<ReservationWriteDto>(payload), cancellationToken),
            "update_reservation" => await Update(reservations, Read<ReservationWriteDto>(payload), cancellationToken),
            "delete_reservation" => await Remove(reservations, Id(payload), cancellationToken),
            "confirm_reservation" => await reservations.ConfirmReservationAsync(Id(payload), cancellationToken),
            "present_reservation" => await reservations.PresentReservationAsync(Id(payload), cancellationToken),
            "complete_reservation" => await reservations.CompleteReservationAsync(Id(payload), cancellationToken),
            "cancel_reservation" => await reservations.CancelReservationAsync(Id(payload), cancellationToken),
            "reject_reservation" => await reservations.RejectReservationAsync(Id(payload), cancellationToken),
            "expire_reservation" => await reservations.ExpireReservationAsync(Id(payload), cancellationToken),
            "move_reservation" => await reservations.MoveReservationAsync(Id(payload), GuidValue(payload, "targetShiftId"), Date(payload, "targetDate"), cancellationToken),
            "list_shifts" => await List(shifts, Read<ShiftDescriptorDto>(payload), cancellationToken),
            "get_shift" => await Get(shifts, Id(payload), cancellationToken),
            "count_shifts" => await Count(shifts, Read<ShiftDescriptorDto>(payload), cancellationToken),
            "create_shift" => await Add(shifts, Read<ShiftWriteDto>(payload), cancellationToken),
            "update_shift" => await Update(shifts, Read<ShiftWriteDto>(payload), cancellationToken),
            "delete_shift" => await Remove(shifts, Id(payload), cancellationToken),
            "activate_shift" => await shifts.ActivateShiftAsync(Id(payload), cancellationToken),
            "pause_shift" => await shifts.PauseShiftAsync(Id(payload), cancellationToken),
            "close_shift" => await shifts.CloseShiftAsync(Id(payload), cancellationToken),
            "cancel_shift" => await shifts.CancelShiftAsync(Id(payload), cancellationToken),
            "archive_shift" => await shifts.ArchiveShiftAsync(Id(payload), cancellationToken),
            "list_services" => await List(services, Read<ServiceDescriptorDto>(payload), cancellationToken),
            "get_service" => await Get(services, Id(payload), cancellationToken),
            "count_services" => await Count(services, Read<ServiceDescriptorDto>(payload), cancellationToken),
            "create_service" => await Add(services, Read<ServiceWriteDto>(payload), cancellationToken),
            "update_service" => await Update(services, Read<ServiceWriteDto>(payload), cancellationToken),
            "delete_service" => await Remove(services, Id(payload), cancellationToken),
            "list_service_types" => await List(serviceTypes, Read<ServiceTypeDescriptorDto>(payload), cancellationToken),
            "get_service_type" => await Get(serviceTypes, Id(payload), cancellationToken),
            "count_service_types" => await Count(serviceTypes, Read<ServiceTypeDescriptorDto>(payload), cancellationToken),
            "create_service_type" => await Add(serviceTypes, Read<ServiceTypeWriteDto>(payload), cancellationToken),
            "update_service_type" => await Update(serviceTypes, Read<ServiceTypeWriteDto>(payload), cancellationToken),
            "delete_service_type" => await Remove(serviceTypes, Id(payload), cancellationToken),
            "list_providers" => await List(providers, Read<ServiceProviderDescriptorDto>(payload), cancellationToken),
            "get_provider" => await Get(providers, Id(payload), cancellationToken),
            "count_providers" => await Count(providers, Read<ServiceProviderDescriptorDto>(payload), cancellationToken),
            "create_provider" => await Add(providers, Read<ServiceProviderWriteDto>(payload), cancellationToken),
            "update_provider" => await Update(providers, Read<ServiceProviderWriteDto>(payload), cancellationToken),
            "delete_provider" => await Remove(providers, Id(payload), cancellationToken),
            "link_provider_user" => await providers.LinkUserAsync(Id(payload), RequiredString(payload, "userId"), cancellationToken),
            "unlink_provider_user" => await providers.UnlinkUserAsync(Id(payload), RequiredString(payload, "userId"), cancellationToken),
            "list_booking_policies" => await List(bookingPolicies, Read<ShiftBookingPolicyDescriptorDto>(payload), cancellationToken),
            "get_booking_policy" => await Get(bookingPolicies, Id(payload), cancellationToken),
            "count_booking_policies" => await Count(bookingPolicies, Read<ShiftBookingPolicyDescriptorDto>(payload), cancellationToken),
            "create_booking_policy" => await Add(bookingPolicies, Read<ShiftBookingPolicyWriteDto>(payload), cancellationToken),
            "update_booking_policy" => await Update(bookingPolicies, Read<ShiftBookingPolicyWriteDto>(payload), cancellationToken),
            "delete_booking_policy" => await Remove(bookingPolicies, Id(payload), cancellationToken),
            "list_reminder_policies" => await List(reminderPolicies, Read<ShiftReminderPolicyDescriptorDto>(payload), cancellationToken),
            "get_reminder_policy" => await Get(reminderPolicies, Id(payload), cancellationToken),
            "count_reminder_policies" => await Count(reminderPolicies, Read<ShiftReminderPolicyDescriptorDto>(payload), cancellationToken),
            "create_reminder_policy" => await Add(reminderPolicies, Read<ShiftReminderPolicyWriteDto>(payload), cancellationToken),
            "update_reminder_policy" => await Update(reminderPolicies, Read<ShiftReminderPolicyWriteDto>(payload), cancellationToken),
            "delete_reminder_policy" => await Remove(reminderPolicies, Id(payload), cancellationToken),
            "list_bookable_dates" => await bookableDates.GetBookableDatesAsync(Id(payload), new ShiftDateRangeQuery { DateFrom = Date(payload, "dateFrom"), DateTo = Date(payload, "dateTo") }, cancellationToken),
            "list_bookable_slots" => await bookableDates.GetBookableSlotsAsync(new ShiftDescriptorDto { Id = Id(payload) }, new ShiftDateRangeQuery { DateFrom = Date(payload, "dateFrom"), DateTo = Date(payload, "dateTo") }, cancellationToken),
            "create_hold" => await CreateHold(payload, cancellationToken),
            "confirm_hold" => await holds.ConfirmHoldAsync(Id(payload), cancellationToken),
            "release_hold" => await holds.ReleaseHoldAsync(Id(payload), cancellationToken),
            _ => throw new JsonException($"Unsupported appointment operation: {operation}.")
        };
        return Result(call, result);
    }

    public ClinicWorkspacePreparedAction? GetPreparedAction(AgentToolResult result)
    {
        using JsonDocument document = JsonDocument.Parse(result.Output);
        JsonElement root = document.RootElement;
        return root.TryGetProperty("status", out JsonElement status) && status.ValueKind == JsonValueKind.String && status.GetString() == "prepared" &&
            root.TryGetProperty("actionId", out JsonElement actionId) && actionId.TryGetGuid(out Guid id) &&
            root.TryGetProperty("arguments", out JsonElement arguments) && arguments.ValueKind == JsonValueKind.String &&
            root.TryGetProperty("expiresAt", out JsonElement expiresAt) && expiresAt.TryGetDateTimeOffset(out DateTimeOffset expiry)
            ? new("Operations", "appointment_manage", id, arguments.GetString()!, expiry)
            : null;
    }

    private async Task<object> CreateHold(JsonElement payload, CancellationToken ct)
    {
        TimeSpan duration = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("durationMinutes", out JsonElement minutes) && minutes.ValueKind == JsonValueKind.Number && minutes.TryGetInt32(out int durationMinutes)
            ? TimeSpan.FromMinutes(durationMinutes)
            : TimeSpan.FromMinutes(15);
        EntityResult<Guid> result = await holds.CreateHoldAsync(Id(payload), Date(payload, "date"), OptionalString(payload, "reservingPartyId"), duration, ct);
        return result;
    }

    private static AgentToolResult Result(AgentToolCall call, object value)
    {
        string output = JsonSerializer.Serialize(value);
        using JsonDocument document = JsonDocument.Parse(output);
        bool isFailure = document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("IsSuccess", out JsonElement isSuccess) &&
            isSuccess.ValueKind == JsonValueKind.False;
        return new(call.Id, output, isFailure);
    }

    private static async Task<object> Get<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, Guid id, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto =>
        (await service.GetAsync(id, ct)).Value ?? throw new InvalidOperationException("Appointment resource was not found.");
    private static async Task<object> List<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TDescriptor descriptor, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto => (await service.GetListAsync(descriptor, ct)).Value ?? [];
    private static async Task<object> Count<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TDescriptor descriptor, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto => (await service.CountAsync(descriptor, ct)).Value;
    private static async Task<object> Add<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TWrite value, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto => (await service.AddAsync(value, ct)).Value ?? throw new InvalidOperationException("Appointment resource could not be created.");
    private static async Task<object> Update<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TWrite value, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto => (await service.UpdateAsync(value, ct)).Value ?? throw new InvalidOperationException("Appointment resource could not be updated.");
    private static async Task<object> Remove<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, Guid id, CancellationToken ct) where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto => await service.RemoveAsync(id, ct);
    private static T Read<T>(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Activator.CreateInstance<T>() : JsonSerializer.Deserialize<T>(value.GetRawText(), PayloadJsonOptions) ?? throw new JsonException("payload is invalid.");
    private static Guid Id(JsonElement payload) => GuidValue(payload, "id");
    private static Guid GuidValue(JsonElement payload, string name) => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out JsonElement value) && value.TryGetGuid(out Guid id) ? id : throw new JsonException($"{name} must be a valid GUID.");
    private static DateOnly Date(JsonElement payload, string name) => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && DateOnly.TryParse(value.GetString(), out DateOnly date) ? date : throw new JsonException($"{name} must be a valid date.");
    private static string RequiredString(JsonElement payload, string name) => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : throw new JsonException($"{name} is required.");
    private static string? OptionalString(JsonElement payload, string name) => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
