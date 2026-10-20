using System.Text.Json;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.E2e.Infrastructure.Communications;

/// <summary>
/// Stands in for the SMS provider: each message becomes one JSON line in the capture file the
/// two-host runner names, so browser specs can read the code a phone sign-in sent. Nothing leaves
/// the machine, and only this E2E composition registers it.
/// </summary>
internal sealed class E2eSmsCapture(string capturePath) : ISmsProvider
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private long nextMessageId;

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
        CaptureAsync(message.Recipient, message.Body, cancellationToken);

    public async Task SendCode(Message message, CancellationToken cancellationToken = default) =>
        _ = await CaptureAsync(message.Recipient, message.Body, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SmsDeliveryState(messageId.ToString(), null, null, "Delivered", null, 0, null, null, null));

    public async Task SendReminder(string recipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default) =>
        _ = await CaptureAsync(recipient, string.Join(" ", new[] { project, procedure, evnt, tenant }.Where(part => !string.IsNullOrWhiteSpace(part))), cancellationToken);

    public Task<SmsSendResult> SendTemplatedSms(string recipient, string templateId, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default) =>
        CaptureAsync(recipient, parameters.TryGetValue(VerificationCodeSmsTemplate.CodeParameter, out string? code) ? code : string.Join(" ", parameters.Values), cancellationToken);

    private async Task<SmsSendResult> CaptureAsync(string? recipient, string? text, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        string messageId = Interlocked.Increment(ref nextMessageId).ToString();
        string line = JsonSerializer.Serialize(new CapturedSms(recipient, text ?? string.Empty)) + Environment.NewLine;
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(capturePath, line, cancellationToken);
        }
        finally
        {
            _ = WriteLock.Release();
        }
        return new SmsSendResult(messageId, recipient, 0, null, DateTime.UtcNow, null, null);
    }

    private sealed record CapturedSms(string Recipient, string Text);
}
