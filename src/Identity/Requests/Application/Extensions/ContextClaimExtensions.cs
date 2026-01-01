using Duende.IdentityModel;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public static class ContextClaimExtensions
{
    private static ClaimsIdentity? GetPartyIdentity(HttpContext context)
        => context.User.Identities.FirstOrDefault(i =>
            string.Equals(i.AuthenticationType, IdentityDefinitions.PartyScope, StringComparison.Ordinal));

    private static string? GetClaimValue(HttpContext context, string claimType)
    {
        ClaimsIdentity? partyIdentity = GetPartyIdentity(context);
        return partyIdentity?.FindFirst(claimType)?.Value
               ?? context.User.FindFirst(claimType)?.Value;
    }
    private static string? GetClaimValue(ClaimsPrincipal principal, string claimType) => principal.FindFirst(claimType)?.Value;
    extension(ClaimsPrincipal principal)
    {
        public string GetUserId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, IdentityDefinitions.UserIdClaim);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{IdentityDefinitions.UserIdClaim}'.");
        }

        public string GetUserGivenName()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, JwtClaimTypes.GivenName);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.GivenName}'.");
        }

        public string GetUserFamilyName()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, JwtClaimTypes.FamilyName);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.FamilyName}'.");
        }

        public string GetUserEmail()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, JwtClaimTypes.Email);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.Email}'.");
        }

        public string GetUserPhoneNumber()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, JwtClaimTypes.PhoneNumber);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.PhoneNumber}'.");
        }

        public string GetClientId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, IdentityDefinitions.ClientIdClaim);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{IdentityDefinitions.ClientIdClaim}'.");
        }

        public string GetOwnerId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, IdentityDefinitions.OwnerIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.OwnerIdClaim}' (expected Guid).");
        }

        public string GetTenantId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? value = GetClaimValue(principal, IdentityDefinitions.TenantIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.TenantIdClaim}' (expected Guid).");
        }


        public bool TryGetUserId(out string userId)
        {
            userId = "";
            if (principal is null)
            {
                return false;
            }

            string? value = GetClaimValue(principal, IdentityDefinitions.UserIdClaim);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            userId = value;
            return true;
        }



        public bool TryGetClientId(out string clientId)
        {
            clientId = "";
            if (principal is null)
            {
                return false;
            }

            string? value = GetClaimValue(principal, IdentityDefinitions.ClientIdClaim);
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
            if (principal is null)
            {
                return false;
            }

            string? value = GetClaimValue(principal, IdentityDefinitions.OwnerIdClaim);
            return Guid.TryParse(value, out ownerId);
        }

        public bool TryGetTenantId(out Guid tenantId)
        {
            tenantId = default;
            if (principal is null)
            {
                return false;
            }

            string? value = GetClaimValue(principal, IdentityDefinitions.TenantIdClaim);
            return Guid.TryParse(value, out tenantId);
        }
    }

    extension(HttpContext context)
    {
        public string GetUserId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, IdentityDefinitions.UserIdClaim);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{IdentityDefinitions.UserIdClaim}'.");
        }

        public string GetUserGivenName()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, JwtClaimTypes.GivenName);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.GivenName}'.");
        }

        public string GetUserFamilyName()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, JwtClaimTypes.FamilyName);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.FamilyName}'.");
        }

        public string GetUserEmail()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, JwtClaimTypes.Email);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.Email}'.");
        }

        public string GetUserPhoneNumber()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, JwtClaimTypes.PhoneNumber);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{JwtClaimTypes.PhoneNumber}'.");
        }


        public string GetClientId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, IdentityDefinitions.ClientIdClaim);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Missing claim '{IdentityDefinitions.ClientIdClaim}'.");
        }

        public string GetOwnerId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, IdentityDefinitions.OwnerIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.OwnerIdClaim}' (expected Guid).");
        }

        public string GetTenantId()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? value = GetClaimValue(context, IdentityDefinitions.TenantIdClaim);
            return value is not null
                ? value
                : throw new InvalidOperationException(
                $"Missing/invalid claim '{IdentityDefinitions.TenantIdClaim}' (expected Guid).");
        }


        public bool TryGetUserId(out string userId)
        {
            userId = "";
            if (context is null)
            {
                return false;
            }

            string? value = GetClaimValue(context, IdentityDefinitions.UserIdClaim);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            userId = value;
            return true;
        }


        public bool TryGetClientId(out string clientId)
        {
            clientId = "";
            if (context is null)
            {
                return false;
            }

            string? value = GetClaimValue(context, IdentityDefinitions.ClientIdClaim);
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

            string? value = GetClaimValue(context, IdentityDefinitions.OwnerIdClaim);
            return Guid.TryParse(value, out ownerId);
        }

        public bool TryGetTenantId(out Guid tenantId)
        {
            tenantId = default;
            if (context is null)
            {
                return false;
            }

            string? value = GetClaimValue(context, IdentityDefinitions.TenantIdClaim);
            return Guid.TryParse(value, out tenantId);
        }
    }
}
