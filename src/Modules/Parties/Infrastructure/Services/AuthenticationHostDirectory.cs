using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class AuthenticationHostDirectory(
    PartiesDbContext database, ISender sender, IHttpContextAccessor accessor,
    IOptions<SignInHostOptions> signIn, IOptions<OnboardingHostOptions> onboarding,
    IOptions<PlatformAdminHostOptions> admin, IOptions<BrowserApplicationHostOptions> applications,
    IEnumerable<IRoleFreeSessionPolicy> roleFreePolicies,
    IEnumerable<IAuthenticationApplicationAdmission>? applicationAdmissions = null) : IAuthenticationHostDirectory
{
    public string? SignInOrigin => signIn.Value.SignInUrl is { } value ? new Uri(value).GetLeftPart(UriPartial.Authority) : null;
    public string CurrentOrigin => accessor.HttpContext?.Request.Headers.Origin.ToString() ?? string.Empty;
    public Task<bool> IsCentralHostAsync(CancellationToken cancellationToken) => sender.Send(new IsSignInHostQuery(), cancellationToken);

    public async Task<bool> CanOpenAsync(string origin, string? userId, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? address) ||
            address.GetLeftPart(UriPartial.Authority) != origin || !string.IsNullOrEmpty(address.UserInfo) ||
            (address.Scheme != Uri.UriSchemeHttps && !(address.Scheme == Uri.UriSchemeHttp && SignInOrigin is { } central && new Uri(central).Scheme == Uri.UriSchemeHttp &&
                (BrowserApplicationHostOptions.IsDevelopmentHostname(address.Host)))))
            return false;
        string?[] configured = [signIn.Value.SignInUrl, onboarding.Value.OnboardingUrl, admin.Value.PlatformAdminUrl,
            applications.Value.PatientsUrl, applications.Value.ProjectManagementUrl, applications.Value.KnowledgeQaUrl];
        bool local = BrowserApplicationHostOptions.IsDevelopmentHostname(address.Host);
        bool configuredOrigin = configured.Where(value => value is not null)
            .Any(value => new Uri(value!).GetLeftPart(UriPartial.Authority) == origin);
        if (configured.Where(value => value is not null).Any(value => new Uri(value!).Host == address.Host) && !configuredOrigin)
            return false;
        bool localDeploymentPort = local && SignInOrigin is { } centralOrigin &&
            new Uri(centralOrigin).Port == address.Port && new Uri(centralOrigin).Scheme == address.Scheme;
        if (!configuredOrigin && !address.IsDefaultPort && !localDeploymentPort)
            return false;
        var tenant = await database.TenantIndicators.AsNoTracking()
            .Where(item => item.Value == address.Host && item.Status == IndicatorStatus.Active && item.Tenant.DeletedAt == null)
            .Select(item => new { item.TenantId, item.Tenant.AppId }).FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return false;
        if (userId is null)
            return true;
        bool roleFree = false;
        foreach (IRoleFreeSessionPolicy policy in roleFreePolicies)
        {
            if (await policy.AllowsRoleFreeSessionForTenantAsync(tenant.TenantId, cancellationToken))
            {
                roleFree = true;
                break;
            }
        }
        IEnumerable<IdentityRole> roles = await sender.Send(new GetUserRolesQuery(userId), cancellationToken);
        IReadOnlySet<string> roleNames = roles.Select(role => role.Name).OfType<string>().ToHashSet(StringComparer.Ordinal);
        foreach (IAuthenticationApplicationAdmission admission in applicationAdmissions ?? [])
            if (admission.Allows(origin, tenant.TenantId, roleNames) is bool allowed) return allowed;
        string targetAppId = applications.Value.PatientsUrl is { } patients && new Uri(patients).GetLeftPart(UriPartial.Authority) == origin
            ? BrowserApplicationTenants.PatientsAppId : tenant.AppId;
        return BrowserApplicationAccessPolicy.Allows(tenant.TenantId, targetAppId,
            roleNames, roleFree,
            onboarding.Value.TenantOnboardingEnabled);
    }
}
