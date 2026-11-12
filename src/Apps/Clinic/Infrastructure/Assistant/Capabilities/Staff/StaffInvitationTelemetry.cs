using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>Staff-invitation log events; the contact only ever appears masked.</summary>
internal static partial class StaffInvitationTelemetry
{
    [LoggerMessage(EventId = 73720, Level = LogLevel.Information,
        Message = "Clinic staff invitation prepared for {MaskedContact}; call {ToolCallId}.")]
    internal static partial void Prepared(ILogger logger, string maskedContact, string toolCallId);

    [LoggerMessage(EventId = 73721, Level = LogLevel.Information,
        Message = "Clinic staff invitation for {MaskedContact} ended with {Outcome}; action {ActionId}.")]
    internal static partial void Committed(ILogger logger, string maskedContact, string outcome, Guid actionId);
}
