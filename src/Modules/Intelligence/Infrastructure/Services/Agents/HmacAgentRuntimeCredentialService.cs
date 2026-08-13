using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

public sealed class HmacAgentRuntimeCredentialService(
    IOptions<AgentRuntimeOptions> options,
    TimeProvider timeProvider) : IAgentRuntimeCredentialService
{
    private static readonly TimeSpan MaximumCredentialLifetime = TimeSpan.FromMinutes(15);

    public string Create(AgentRuntimeExecutionEnvelope envelope)
    {
        ValidateConfiguration();
        ValidateEnvelope(envelope, requireToolCall: false);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (envelope.ExpiresAt <= now || envelope.ExpiresAt > now.Add(MaximumCredentialLifetime))
            throw new InvalidOperationException("The agent runtime credential expiry must be short-lived and in the future.");
        long expiresAt = envelope.ExpiresAt.ToUnixTimeSeconds();
        return $"{expiresAt}.{Encode(Sign(Canonical(BaseEnvelope(envelope))))}";
    }

    public bool Validate(string credential, AgentRuntimeExecutionEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SharedSecret) || !TryReadExpiry(credential, out long credentialExpiry)) return false;
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (credentialExpiry <= now.ToUnixTimeSeconds() || credentialExpiry > now.Add(MaximumCredentialLifetime).ToUnixTimeSeconds() ||
            envelope.ExpiresAt <= now || envelope.ExpiresAt.ToUnixTimeSeconds() != credentialExpiry) return false;
        try { ValidateEnvelope(envelope, requireToolCall: false); }
        catch (InvalidOperationException) { return false; }

        byte[] expected = Sign(Canonical(BaseEnvelope(envelope)));
        return TryDecodeCredentialSignature(credential, expected);
    }

    public bool ValidateCallback(string credential, AgentRuntimeExecutionEnvelope envelope)
    {
        if (!Validate(credential, envelope) || string.IsNullOrWhiteSpace(envelope.CallbackSignature) ||
            string.IsNullOrWhiteSpace(envelope.ToolCallId) || string.IsNullOrWhiteSpace(envelope.ToolName))
            return false;
        byte[] expected = SignWithKey(credential, Canonical(envelope));
        byte[] actual;
        try { actual = Decode(envelope.CallbackSignature); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static AgentRuntimeExecutionEnvelope BaseEnvelope(AgentRuntimeExecutionEnvelope envelope) =>
        envelope with { ToolCallId = "", ToolName = "", CallbackSignature = null };

    private static string Canonical(AgentRuntimeExecutionEnvelope envelope) => string.Join('\n',
        envelope.Version.ToString(CultureInfo.InvariantCulture),
        envelope.ExecutionId,
        envelope.AgentKey,
        envelope.TenantId,
        envelope.PracticeId,
        envelope.SubjectKind,
        envelope.SubjectId,
        envelope.AuthenticatedUserId,
        envelope.ConversationId,
        envelope.ToolCallId,
        envelope.ToolName,
        envelope.CredentialVersion.ToString(CultureInfo.InvariantCulture),
        envelope.ExpiresAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        envelope.CorrelationId,
        envelope.Role,
        envelope.Locale,
        envelope.TimeZone,
        envelope.Calendar,
        envelope.CurrentUtc,
        envelope.CurrentLocal,
        JsonSerializer.Serialize(envelope.Permissions.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
        envelope.PatientNationalId ?? string.Empty);

    private byte[] Sign(string payload) => SignWithKey(options.Value.SharedSecret, payload);

    private static byte[] SignWithKey(string key, string payload) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(payload));

    private bool TryDecodeCredentialSignature(string credential, byte[] expected)
    {
        string[] parts = credential.Split('.', 2, StringSplitOptions.None);
        if (parts.Length != 2) return false;
        byte[] actual;
        try { actual = Decode(parts[1]); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static bool TryReadExpiry(string credential, out long expiresAt)
    {
        expiresAt = 0;
        string[] parts = credential.Split('.', 2, StringSplitOptions.None);
        return parts.Length == 2 && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out expiresAt);
    }

    private static void ValidateEnvelope(AgentRuntimeExecutionEnvelope envelope, bool requireToolCall)
    {
        if (envelope.Version != AgentRuntimeExecutionEnvelope.CurrentVersion ||
            envelope.CredentialVersion != AgentRuntimeExecutionEnvelope.CurrentCredentialVersion ||
            string.IsNullOrWhiteSpace(envelope.ExecutionId) || string.IsNullOrWhiteSpace(envelope.AgentKey) ||
            string.IsNullOrWhiteSpace(envelope.TenantId) || string.IsNullOrWhiteSpace(envelope.PracticeId) ||
            string.IsNullOrWhiteSpace(envelope.SubjectKind) || string.IsNullOrWhiteSpace(envelope.SubjectId) ||
            string.IsNullOrWhiteSpace(envelope.AuthenticatedUserId) || string.IsNullOrWhiteSpace(envelope.ConversationId) ||
            string.IsNullOrWhiteSpace(envelope.CorrelationId) || string.IsNullOrWhiteSpace(envelope.Role) ||
            string.IsNullOrWhiteSpace(envelope.Locale) || string.IsNullOrWhiteSpace(envelope.TimeZone) ||
            string.IsNullOrWhiteSpace(envelope.Calendar) || envelope.Permissions is null ||
            !DateTimeOffset.TryParse(envelope.CurrentUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) ||
            !DateTimeOffset.TryParse(envelope.CurrentLocal, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) ||
            envelope.ExpiresAt <= DateTimeOffset.UnixEpoch ||
            requireToolCall && (string.IsNullOrWhiteSpace(envelope.ToolCallId) || string.IsNullOrWhiteSpace(envelope.ToolName)))
            throw new InvalidOperationException("The agent runtime execution envelope is incomplete.");
        if (envelope.SubjectKind is not ("patient" or "practice" or "event" or "landing-page"))
            throw new InvalidOperationException("The agent runtime subject kind is invalid.");
        if (envelope.SubjectKind == "patient")
        {
            if (!Guid.TryParse(envelope.SubjectId, out Guid patientId) || patientId == Guid.Empty)
                throw new InvalidOperationException("The agent runtime patient scope is invalid.");
        }
        else if (envelope.SubjectKind == "practice" &&
            (envelope.SubjectId != envelope.PracticeId || envelope.TenantId != envelope.PracticeId))
        {
            throw new InvalidOperationException("The agent runtime practice scope is invalid.");
        }
        else if (envelope.SubjectKind == "event" &&
            (!Guid.TryParse(envelope.SubjectId, out Guid eventId) || eventId == Guid.Empty))
        {
            throw new InvalidOperationException("The agent runtime event scope is invalid.");
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(options.Value.SharedSecret))
            throw new InvalidOperationException("Vitarish:Intelligence:AgentRuntime:SharedSecret must be configured.");
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
}
