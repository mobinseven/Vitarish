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
        public string? GetUserId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, IdentityDefinitions.UserIdClaim);
        }

        public string? GetUserGivenName()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, JwtClaimTypes.GivenName);
        }

        public string? GetUserFullName()
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? given = GetClaimValue(principal, JwtClaimTypes.GivenName);
            string? sure = GetClaimValue(principal, JwtClaimTypes.FamilyName);

            return string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(sure) ? null : $"{given} {sure}".Trim();
        }

        public string? GetUserFamilyName()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, JwtClaimTypes.FamilyName);
        }

        public string? GetUserEmail()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, JwtClaimTypes.Email);
        }

        public string? GetUserPhoneNumber()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, JwtClaimTypes.PhoneNumber);
        }

        public string? GetClientId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, IdentityDefinitions.ClientIdClaim);
        }

        public string? GetOwnerId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, IdentityDefinitions.OwnerIdClaim);
        }

        public string? GetTenantId()
        {
            ArgumentNullException.ThrowIfNull(principal);

            return GetClaimValue(principal, IdentityDefinitions.TenantIdClaim);
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
        public string? GetUserId()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, IdentityDefinitions.UserIdClaim);
        }

        public string? GetUserGivenName()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, JwtClaimTypes.GivenName);
        }

        public string? GetUserFamilyName()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, JwtClaimTypes.FamilyName);
        }

        public string? GetUserFullName()
        {
            ArgumentNullException.ThrowIfNull(context);

            string? given = GetClaimValue(context, JwtClaimTypes.GivenName);
            string? sure = GetClaimValue(context, JwtClaimTypes.FamilyName);

            return string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(sure) ? null : $"{given} {sure}".Trim();
        }

        public string? GetUserEmail()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, JwtClaimTypes.Email);
        }

        public string? GetUserPhoneNumber()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, JwtClaimTypes.PhoneNumber);
        }


        public string? GetClientId()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, IdentityDefinitions.ClientIdClaim);
        }

        public string? GetOwnerId()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, IdentityDefinitions.OwnerIdClaim);
        }

        public string? GetTenantId()
        {
            ArgumentNullException.ThrowIfNull(context);

            return GetClaimValue(context, IdentityDefinitions.TenantIdClaim);
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
