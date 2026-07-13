namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationSessionDto(bool IsAuthenticated, CurrentAuthenticationUserDto? User, TenantAccessDto TenantAccess, PlatformAccessDto PlatformAccess);
