using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Agents;

namespace Intelligence.Tests.Agents;

public sealed class HmacAgentRuntimeCredentialServiceTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero));
    [Fact]
    public void Validate_AcceptsCredentialForTheSameEnvelope()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        AgentRuntimeExecutionEnvelope envelope = Envelope(time.GetUtcNow().AddMinutes(5));

        string credential = service.Create(envelope);

        service.Validate(credential, envelope).Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsChangedEnvelope()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        AgentRuntimeExecutionEnvelope envelope = Envelope(time.GetUtcNow().AddMinutes(5));
        string credential = service.Create(envelope);

        service.Validate(credential, envelope with { SubjectId = "22222222-2222-2222-2222-222222222222" }).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsExpiredCredential()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        AgentRuntimeExecutionEnvelope envelope = Envelope(time.GetUtcNow().AddMinutes(5));
        string credential = service.Create(envelope);
        time.Advance(TimeSpan.FromMinutes(5));

        service.Validate(credential, envelope).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsMalformedCredential()
    {
        CreateService().Validate("not-a-credential", Envelope(time.GetUtcNow().AddMinutes(5))).Should().BeFalse();
    }

    [Fact]
    public void ValidateCallback_BindsToolCallAndToolNameToTheCredential()
    {
        HmacAgentRuntimeCredentialService service = CreateService();
        AgentRuntimeExecutionEnvelope envelope = AgentRuntimeExecutionEnvelope.FromContext(
            new AgentExecutionContext("execution-1", new Dictionary<string, string>
            {
                ["practiceId"] = "practice-1",
                ["tenantId"] = "practice-1",
                ["patientId"] = "11111111-1111-1111-1111-111111111111",
                ["subjectKind"] = "patient",
                ["subjectId"] = "11111111-1111-1111-1111-111111111111",
                ["authenticatedUserId"] = "user-1",
                ["conversationId"] = "conversation-1",
                ["correlationId"] = "execution-1",
                ["agentKey"] = "clinic.assistant.patient",
                ["role"] = "Clinician",
                ["locale"] = "en-US",
                ["timeZone"] = "UTC",
                ["calendar"] = "gregory",
                ["currentUtc"] = time.GetUtcNow().ToString("O"),
                ["currentLocal"] = time.GetUtcNow().ToString("O"),
                ["permissions"] = "[\"Patient.Read\"]"
            }),
            time.GetUtcNow().AddMinutes(5)) with
        {
            ToolCallId = "call-1",
            ToolName = "read_patient"
        };
        string credential = service.Create(envelope);
        AgentRuntimeExecutionEnvelope callback = envelope with { CallbackSignature = SignCallback(credential, envelope) };

        service.ValidateCallback(credential, callback).Should().BeTrue();
        AgentRuntimeExecutionEnvelope[] mismatches =
        [
            callback with { Version = 2 },
            callback with { ExecutionId = "execution-2" },
            callback with { AgentKey = "clinic.assistant.operations" },
            callback with { TenantId = "practice-2" },
            callback with { PracticeId = "practice-2" },
            callback with { SubjectKind = "practice" },
            callback with { SubjectId = "22222222-2222-2222-2222-222222222222" },
            callback with { AuthenticatedUserId = "user-2" },
            callback with { ConversationId = "conversation-2" },
            callback with { ToolCallId = "call-2" },
            callback with { ToolName = "read_other_patient" },
            callback with { CredentialVersion = 2 },
            callback with { CorrelationId = "correlation-2" },
            callback with { CallbackSignature = "invalid" }
        ];

        foreach (AgentRuntimeExecutionEnvelope mismatch in mismatches)
            service.ValidateCallback(credential, mismatch).Should().BeFalse();
    }

    [Fact]
    public void Create_RequiresSharedSecret()
    {
        HmacAgentRuntimeCredentialService service = new(
            Options.Create(new AgentRuntimeOptions()),
            time);

        FluentActions.Invoking(() => service.Create(Envelope(time.GetUtcNow().AddMinutes(5))))
            .Should().Throw<InvalidOperationException>();
    }

    private HmacAgentRuntimeCredentialService CreateService() => new(
        Options.Create(new AgentRuntimeOptions { SharedSecret = "test-secret" }),
        time);

    private AgentRuntimeExecutionEnvelope Envelope(DateTimeOffset expiresAt) => new(
        AgentRuntimeExecutionEnvelope.CurrentVersion,
        "execution-1",
        "clinic.assistant.patient",
        "practice-1",
        "practice-1",
        "patient",
        "11111111-1111-1111-1111-111111111111",
        "user-1",
        "conversation-1",
        "",
        "",
        AgentRuntimeExecutionEnvelope.CurrentCredentialVersion,
        expiresAt,
        "execution-1",
        "Clinician",
        "en-US",
        "UTC",
        "gregory",
        time.GetUtcNow().ToString("O"),
        time.GetUtcNow().ToString("O"),
        ["Patient.Read"]);

    private static string SignCallback(string credential, AgentRuntimeExecutionEnvelope envelope)
    {
        string canonical = string.Join('\n',
            envelope.Version.ToString(CultureInfo.InvariantCulture), envelope.ExecutionId, envelope.AgentKey,
            envelope.TenantId, envelope.PracticeId, envelope.SubjectKind, envelope.SubjectId,
            envelope.AuthenticatedUserId, envelope.ConversationId, envelope.ToolCallId, envelope.ToolName,
            envelope.CredentialVersion.ToString(CultureInfo.InvariantCulture),
            envelope.ExpiresAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), envelope.CorrelationId,
            envelope.Role, envelope.Locale, envelope.TimeZone, envelope.Calendar, envelope.CurrentUtc,
            envelope.CurrentLocal, JsonSerializer.Serialize(envelope.Permissions.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
            envelope.PatientNationalId ?? string.Empty);
        byte[] signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(credential), Encoding.UTF8.GetBytes(canonical));
        return Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
