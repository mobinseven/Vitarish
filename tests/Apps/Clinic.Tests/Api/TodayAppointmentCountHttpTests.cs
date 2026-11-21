using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Queries.Dashboard;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Clinic.Infrastructure.Assistant;
using Vitarish.Modules.Base.Core.Endpoints;
using Xunit;

namespace Clinic.Tests.Api;

/// <summary>Exercises the real endpoint, authorization pipeline, context factory and query handler over HTTP.</summary>
public sealed class TodayAppointmentCountHttpTests
{
    private const string PracticeOne = "practice-1";
    private const string PracticeTwo = "practice-2";
    private const string TestUserHeader = "X-Test-User";
    private const string TestTenantHeader = "X-Test-Tenant";
    private const string TestRoleHeader = "X-Test-Role";
    private const string TimeZoneHeader = "X-Time-Zone";
    private const string TenantClaimType = "tenant";
    private const string CountUrl = ClinicApiRoutes.Base + ClinicApiRoutes.OperationsTodayAppointmentCount;

    private static string OwnerOf(string practice) => $"{practice}:{AssistantOperationsScope.TenantOwnerRole}";

    [Fact]
    public async Task An_anonymous_request_is_rejected_with_401_and_reads_nothing()
    {
        await using TestApp app = await TestApp.StartAsync();

        HttpResponseMessage response = await app.Client.GetAsync(CountUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        app.VerifyNothingRead();
    }

    [Theory]
    [InlineData("practice-1:TenantUser")]
    [InlineData("")]
    public async Task A_member_who_is_not_an_owner_is_denied_and_reads_nothing(string role)
    {
        await using TestApp app = await TestApp.StartAsync();

        HttpResponseMessage response = await app.GetAsync(PracticeOne, role);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        app.VerifyNothingRead();
    }

    [Fact]
    public async Task An_owner_of_another_practice_is_denied_and_reads_nothing()
    {
        await using TestApp app = await TestApp.StartAsync();

        // Signed in to practice-1 but holding the owner role only for practice-2.
        HttpResponseMessage response = await app.GetAsync(PracticeOne, OwnerOf(PracticeTwo));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        app.VerifyNothingRead();
    }

    [Fact]
    public async Task A_practice_the_appointments_capability_denies_gets_403_and_no_count()
    {
        await using TestApp app = await TestApp.StartAsync(AgentSchedulingStatus.Forbidden, count: 7);

        HttpResponseMessage response = await app.GetAsync(PracticeTwo, OwnerOf(PracticeTwo));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("7");
    }

    [Fact]
    public async Task The_practice_owner_gets_the_count_for_their_own_practice_and_local_day()
    {
        await using TestApp app = await TestApp.StartAsync(count: 4);

        HttpResponseMessage response = await app.GetAsync(PracticeOne, OwnerOf(PracticeOne));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TodayAppointmentCountResponse>())
            .Should().Be(new TodayAppointmentCountResponse(new DateOnly(2026, 11, 21), 4));
        app.Scope!.PracticeId.Should().Be(PracticeOne);
        app.Request!.DateFrom.Should().Be(new DateOnly(2026, 11, 21));
        app.Request.DateTo.Should().Be(new DateOnly(2026, 11, 21));
    }

    [Fact]
    public async Task An_owner_with_no_appointments_gets_an_explicit_zero()
    {
        await using TestApp app = await TestApp.StartAsync(count: 0);

        HttpResponseMessage response = await app.GetAsync(PracticeOne, OwnerOf(PracticeOne));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TodayAppointmentCountResponse>())!.Count.Should().Be(0);
    }

    [Fact]
    public async Task A_read_failure_is_503_instead_of_zero()
    {
        await using TestApp app = await TestApp.StartAsync(AgentSchedulingStatus.Failed);

        HttpResponseMessage response = await app.GetAsync(PracticeOne, OwnerOf(PracticeOne));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private sealed class TestApp : IAsyncDisposable
    {
        private readonly WebApplication application;

        private TestApp(WebApplication application, Mock<IAppointmentSchedulingAgentCapability> scheduling)
        {
            this.application = application;
            Client = application.GetTestClient();
            Scheduling = scheduling;
        }

        public HttpClient Client { get; }
        public Mock<IAppointmentSchedulingAgentCapability> Scheduling { get; }
        public AgentSchedulingContext? Scope { get; private set; }
        public AgentAppointmentListRequest? Request { get; private set; }

        public static async Task<TestApp> StartAsync(AgentSchedulingStatus countStatus = AgentSchedulingStatus.Succeeded, int count = 0)
        {
            Mock<IAppointmentSchedulingAgentCapability> scheduling = new(MockBehavior.Strict);
            TestApp? holder = null;
            scheduling.Setup(item => item.CountAppointmentsAsync(
                    It.IsAny<AgentSchedulingContext>(), It.IsAny<AgentAppointmentListRequest>(), It.IsAny<CancellationToken>()))
                .Callback<AgentSchedulingContext, AgentAppointmentListRequest, CancellationToken>((scope, request, _) =>
                {
                    holder!.Scope = scope;
                    holder.Request = request;
                })
                .ReturnsAsync(new AgentSchedulingResult<int>(countStatus, count));

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            _ = builder.WebHost.UseTestServer();
            _ = builder.Logging.ClearProviders();
            _ = builder.Services.AddHttpContextAccessor();
            _ = builder.Services.AddRouting();
            _ = builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(ModuleEndpointAuthenticationSchemes.Bearer, null)
                .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(ModuleEndpointAuthenticationSchemes.Browser, null);
            _ = builder.Services.AddAuthorization(options =>
                options.AddPolicy(IdentityDefinitions.TenantAccess, policy => policy.RequireClaim(TenantClaimType)));
            _ = builder.Services.AddMediatR(configuration =>
                configuration.RegisterServicesFromAssemblyContaining<GetTodayAppointmentCountQuery>());
            _ = builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero)));
            _ = builder.Services.AddSingleton(scheduling.Object);
            _ = builder.Services.AddSingleton(Mock.Of<IAssistantRoutePatientResolver>());
            _ = builder.Services.AddScoped<IAssistantContextFactory, HttpAssistantContextFactory>();

            WebApplication application = builder.Build();
            _ = application.UseAuthentication();
            _ = application.UseAuthorization();
            _ = application.MapClinicApi();
            await application.StartAsync();

            holder = new TestApp(application, scheduling);
            return holder;
        }

        public Task<HttpResponseMessage> GetAsync(string tenant, string role)
        {
            HttpRequestMessage request = new(HttpMethod.Get, CountUrl);
            request.Headers.Add(TestUserHeader, "user-1");
            request.Headers.Add(TestTenantHeader, tenant);
            request.Headers.Add(TestRoleHeader, role);
            request.Headers.Add(TimeZoneHeader, "Asia/Tehran");
            return Client.SendAsync(request);
        }

        public void VerifyNothingRead() => Scheduling.Verify(item => item.CountAppointmentsAsync(
            It.IsAny<AgentSchedulingContext>(), It.IsAny<AgentAppointmentListRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await application.DisposeAsync();
        }
    }

    /// <summary>Authenticates from test headers; no user header means an anonymous caller.</summary>
    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(TestUserHeader, out StringValues user))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            List<Claim> claims = [new("sub", user.ToString()), new(TenantClaimType, Request.Headers[TestTenantHeader].ToString())];
            string role = Request.Headers[TestRoleHeader].ToString();
            if (role.Length > 0)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            ClaimsPrincipal principal = new(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
