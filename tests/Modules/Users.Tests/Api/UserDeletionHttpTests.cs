using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Api;
using Vitarish.Users.Api.Authorization;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Authorization;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Api;

public sealed class UserDeletionHttpTests
{
    private const string RoleHeader = "X-Test-Role";
    private const string AntiforgeryPath = "/test/antiforgery";
    private const string AntiforgeryHeader = "X-Test-CSRF";
    private const string SubjectId = "http-deletion-subject";
    private const string ActorId = "http-administrator";
    private const string Revision = "http-deletion-revision";
    private static string DeletePath => UsersApiRoutes.Base + UsersApiRoutes.DeleteUser.Replace("{" + UsersApiRoutes.UserIdRouteValue + "}", SubjectId);

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(IdentityDefinitions.PlatformUser, HttpStatusCode.Forbidden)]
    public async Task Direct_http_anonymous_and_ordinary_users_cannot_delete(string? role, HttpStatusCode expected)
    {
        await using Host host = await StartAsync();
        if (role is not null) host.Client.DefaultRequestHeaders.Add(RoleHeader, role);
        HttpResponseMessage response = await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest(Revision));
        response.StatusCode.Should().Be(expected);
        (await host.Database.Users.SingleAsync()).DeletedAt.Should().BeNull();
        host.Events.Should().Contain(item => item.EventName == SecurityAuditEventCatalog.UserDeletionDeniedEvent && item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task A_tenant_owner_cannot_bypass_the_platform_policy_by_calling_the_endpoint()
    {
        await using Host host = await StartAsync();
        host.Client.DefaultRequestHeaders.Add(RoleHeader, TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner));
        (await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest(Revision))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.Database.Users.SingleAsync()).DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Browser_admin_requires_antiforgery_then_deletes_and_replays_without_reviving_the_row()
    {
        await using Host host = await StartAsync();
        host.Client.DefaultRequestHeaders.Add(RoleHeader, IdentityDefinitions.PlatformAdmin);
        (await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest(Revision))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await host.Database.Users.SingleAsync()).DeletedAt.Should().BeNull();
        AntiforgeryTokenSet token = (await host.Client.GetFromJsonAsync<AntiforgeryTokenSet>(AntiforgeryPath))!;
        host.Client.DefaultRequestHeaders.Add(AntiforgeryHeader, token.RequestToken);
        HttpResponseMessage stale = await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest("stale"));
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await stale.Content.ReadFromJsonAsync<UsersCommandResponse>())!.Errors.Should().ContainSingle(error => error.FailureCode == UsersCommandErrorKind.ConcurrencyFailure);
        (await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest(Revision))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await host.Client.PostAsJsonAsync(DeletePath, new DeleteUserRequest(Revision))).StatusCode.Should().Be(HttpStatusCode.OK);
        host.Database.ChangeTracker.Clear();
        (await host.Database.Users.CountAsync()).Should().Be(0);
        (await host.Database.Users.IgnoreQueryFilters().SingleAsync()).IsDeleted.Should().BeTrue();
        host.Events.Should().Contain(item => item.EventName == SecurityAuditEventCatalog.UserDeletionEvent && item.ActorId == ActorId && item.SubjectId == SubjectId);
    }

    private static async Task<Host> StartAsync()
    {
        UsersDbContext database = new(new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        database.Users.Add(new User { Id = SubjectId, UserName = "http@example.test", Email = "http@example.test", ConcurrencyStamp = Revision });
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAntiforgery(options => options.HeaderName = AntiforgeryHeader);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddAuthentication(ModuleEndpointAuthenticationSchemes.Browser)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthentication>(ModuleEndpointAuthenticationSchemes.Browser, _ => { })
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthentication>(ModuleEndpointAuthenticationSchemes.Bearer, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            foreach (string policy in new[] { UsersAuthorizationPolicies.ManageAccounts, UsersAuthorizationPolicies.ReadSecurityState,
                UsersAuthorizationPolicies.AssignGlobalRole, UsersAuthorizationPolicies.ReadTenantRoles, UsersAuthorizationPolicies.ReplaceTenantRole,
                UsersAuthorizationPolicies.ManageTenantMembers })
                options.AddPolicy(policy, policyBuilder => policyBuilder.RequireRole(IdentityDefinitions.PlatformAdmin));
        });
        List<SecurityAuditEvent> events = [];
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((item, _) => events.Add(item)).ReturnsAsync(SecurityAuditWriteResult.Recorded);
        builder.Services.AddSingleton(writer.Object);
        builder.Services.AddSingleton<IRequestContext>(services =>
        {
            IHttpContextAccessor accessor = services.GetRequiredService<IHttpContextAccessor>();
            Mock<IRequestContext> context = new();
            context.SetupGet(value => value.User).Returns(() => accessor.HttpContext?.User);
            context.SetupGet(value => value.UserId).Returns(() => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier));
            context.SetupGet(value => value.RequestId).Returns(() => accessor.HttpContext?.TraceIdentifier);
            return context.Object;
        });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<UsersManagementAudit>();
        builder.Services.AddScoped<IAuthorizationHandler, UsersManagementAuthorizationAuditHandler>();
        builder.Services.AddSingleton(Mock.Of<IUsersService>());
        builder.Services.AddSingleton(Mock.Of<IUserProfileService>());
        builder.Services.AddSingleton(Mock.Of<IPersonalAccessTokenGenerator>());
        builder.Services.AddSingleton(Mock.Of<IWebAuthnService>());
        builder.Services.AddSingleton(Mock.Of<IValidLocaleProvider>());
        builder.Services.AddSingleton(Mock.Of<ITenantExistence>());
        builder.Services.AddScoped<UserSubjectAccessPolicy>();
        builder.Services.AddScoped<ISender>(services =>
        {
            Mock<IAuthenticationActiveSessionQuery> sessions = new();
            DeleteUserCommandHandler handler = new(new UserAccountDeletionStore(database, services.GetRequiredService<IDistributedCache>()),
                sessions.Object, services.GetRequiredService<IRequestContext>(), services.GetRequiredService<UsersManagementAudit>(), TimeProvider.System);
            Mock<ISender> sender = new();
            sender.Setup(value => value.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
                .Returns((DeleteUserCommand command, CancellationToken ct) => handler.Handle(command, ct));
            return sender.Object;
        });
        WebApplication app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseModuleEndpointAntiforgery();
        app.MapGet(AntiforgeryPath, (HttpContext http, IAntiforgery antiforgery) => antiforgery.GetAndStoreTokens(http));
        app.MapUsersApi();
        await app.StartAsync();
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new Host(app, database, new HttpClient { BaseAddress = new Uri(address) }, events);
    }

    private sealed class HeaderAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Scheme.Name == ModuleEndpointAuthenticationSchemes.Bearer || !Request.Headers.TryGetValue(RoleHeader, out var role))
                return Task.FromResult(AuthenticateResult.NoResult());
            ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ActorId), new Claim(ClaimTypes.Role, role.ToString())], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed record Host(WebApplication App, UsersDbContext Database, HttpClient Client, List<SecurityAuditEvent> Events) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
            await Database.DisposeAsync();
        }
    }
}
