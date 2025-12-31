using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public static class PartyClaimsHttpContextExtensions
{
    private static ClaimsIdentity? GetPartyIdentity(HttpContext context)
        => context.User.Identities.FirstOrDefault(i =>
            string.Equals(i.AuthenticationType, IdentityDefinitions.PartyScope, StringComparison.Ordinal));

    private static string? GetPartyClaimValue(HttpContext context, string claimType)
    {
        ClaimsIdentity? partyIdentity = GetPartyIdentity(context);
        return partyIdentity?.FindFirst(claimType)?.Value
               ?? context.User.FindFirst(claimType)?.Value;
    }

    extension(HttpContext context)
    {

        public string GetClientId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetPartyClaimValue(context, IdentityDefinitions.ClientIdClaim);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{IdentityDefinitions.ClientIdClaim}'.");
        }

        public string GetOwnerId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetPartyClaimValue(context, IdentityDefinitions.OwnerIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.OwnerIdClaim}' (expected Guid).");
        }

        public string GetTenantId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetPartyClaimValue(context, IdentityDefinitions.TenantIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.TenantIdClaim}' (expected Guid).");
        }


        public bool TryGetClientId(out string clientId)
        {
            clientId = "";
            if (context is null)
            {
                return false;
            }

            string? value = GetPartyClaimValue(context, IdentityDefinitions.ClientIdClaim);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            clientId = value;
            return true;
        }

        public bool TryGetOwnerId(out Guid ownerId)
        {
            ownerId = default;
            if (context is null)
            {
                return false;
            }

            string? value = GetPartyClaimValue(context, IdentityDefinitions.OwnerIdClaim);
            return Guid.TryParse(value, out ownerId);
        }

        public bool TryGetTenantId(out Guid tenantId)
        {
            tenantId = default;
            if (context is null)
            {
                return false;
            }

            string? value = GetPartyClaimValue(context, IdentityDefinitions.TenantIdClaim);
            return Guid.TryParse(value, out tenantId);
        }
    }
}
