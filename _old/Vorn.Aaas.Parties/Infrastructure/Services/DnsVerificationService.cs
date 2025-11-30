using DnsClient;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Vorn.Aaas.Parties.Infrastructure.Abstractions;

namespace Vorn.Aaas.Parties.Infrastructure.Services;

internal sealed class DnsVerificationService(ILookupClient lookupClient, ILogger<DnsVerificationService> logger) : IDnsVerificationService
{
    public async Task<bool> VerifyTxtRecordAsync(string hostname, string expectedHash, CancellationToken cancellationToken = default)
    {
        IDnsQueryResponse response = await lookupClient.QueryAsync(hostname, QueryType.TXT, cancellationToken: cancellationToken);

        if (!response.HasError && response.Answers.TxtRecords().Any())
        {
            foreach (DnsClient.Protocol.TxtRecord txtRecord in response.Answers.TxtRecords())
            {
                foreach (string txtValue in txtRecord.Text.Where(t => t.StartsWith("verify=", StringComparison.OrdinalIgnoreCase)))
                {
                    string token = txtValue["verify=".Length..];
                    byte[] tokenBytes = Encoding.UTF8.GetBytes($"verify={token}");
                    string hash = Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();

                    if (string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("DNS TXT verification succeeded for {Hostname}", hostname);
                        return true;
                    }
                }
            }
        }

        logger.LogWarning("DNS TXT verification failed for {Hostname}: no matching record found", hostname);
        return false;

    }
}
