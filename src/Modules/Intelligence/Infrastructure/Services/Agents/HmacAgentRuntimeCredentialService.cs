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
    public string Create(AgentExecutionContext context, TimeSpan lifetime)
    {
        ValidateConfiguration();
        long expiresAt = timeProvider.GetUtcNow().Add(lifetime).ToUnixTimeSeconds();
        return $"{expiresAt}.{Encode(Sign(Payload(context, expiresAt)))}";
    }

    public bool Validate(string credential, AgentExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SharedSecret)) return false;
        string[] parts = credential.Split('.', 2, StringSplitOptions.None);
        if (parts.Length != 2 || !long.TryParse(parts[0], out long expiresAt) ||
            expiresAt <= timeProvider.GetUtcNow().ToUnixTimeSeconds()) return false;

        byte[] expected = Sign(Payload(context, expiresAt));
        byte[] actual;
        try { actual = Decode(parts[1]); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string Payload(AgentExecutionContext context, long expiresAt) =>
        string.Join('\n', context.ExecutionId, expiresAt, JsonSerializer.Serialize(
            (context.Metadata ?? new Dictionary<string, string>())
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)));

    private byte[] Sign(string payload) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.SharedSecret), Encoding.UTF8.GetBytes(payload));

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(options.Value.SharedSecret))
            throw new InvalidOperationException("Vitarish:Intelligence:AgentRuntime:SharedSecret must be configured.");
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
}
