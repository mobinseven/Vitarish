using System.Text.Json.Serialization;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Clinic.Application.Assistant.Operations;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;

[JsonSerializable(typeof(IReadOnlyList<AssistantSearchCandidate>))]
[JsonSerializable(typeof(OperationsAgentToolResponse<IReadOnlyList<OperationsAppointmentRead>>))]
[JsonSerializable(typeof(OperationsAgentToolResponse<OperationsAppointmentRead>))]
[JsonSerializable(typeof(AgentSchedulingResult))]
[JsonSerializable(typeof(AgentSchedulingResult<int>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ReservationReadDto>>))]
[JsonSerializable(typeof(AgentSchedulingResult<ReservationReadDto>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<BookableShiftSlot>>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ShiftReadDto>>))]
[JsonSerializable(typeof(AgentSchedulingResult<ShiftReadDto>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ShiftReservationReadDto>>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ServiceReadDto>>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ServiceTypeReadDto>>))]
[JsonSerializable(typeof(AgentSchedulingResult<IReadOnlyList<ServiceProviderReadDto>>))]
[JsonSerializable(typeof(OperationsPreparedActionResult))]
[JsonSerializable(typeof(OperationsActionReview))]
internal sealed partial class OperationsAgentToolJsonSerializerContext : JsonSerializerContext;
