using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Authentication.Module.DependencyInjection;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class AuthenticationSecurityDependencyCompositionTests
{
    public static IEnumerable<object[]> AuthenticationHandlers()
    {
        yield return [typeof(BeginEmailLoginHandler)];
        yield return [typeof(BeginPhoneLoginHandler)];
        yield return [typeof(BeginPasskeyLoginHandler)];
        yield return [typeof(CompleteEmailLoginHandler)];
        yield return [typeof(CompletePhoneLoginHandler)];
        yield return [typeof(CompletePasskeyLoginHandler)];
        yield return [typeof(CompletePasswordLoginOtpHandler)];
        yield return [typeof(LoginWithPasswordHandler)];
        yield return [typeof(BeginPasswordRecoveryHandler)];
    }

    [Theory]
    [MemberData(nameof(AuthenticationHandlers))]
    public void Authentication_handlers_require_non_nullable_security_dependencies(Type handlerType)
    {
        ConstructorInfo constructor = handlerType.GetConstructors().Single();
        NullabilityInfoContext nullability = new();

        constructor.GetParameters()
            .Where(parameter => parameter.ParameterType == typeof(IAuthenticationAttemptTracker) ||
                                parameter.ParameterType == typeof(IAuthenticationSecurityPolicyProvider))
            .Select(parameter => parameter.ParameterType)
            .Should().BeEquivalentTo(new[]
            {
                typeof(IAuthenticationAttemptTracker),
                typeof(IAuthenticationSecurityPolicyProvider)
            });

        constructor.GetParameters()
            .Where(parameter => parameter.ParameterType == typeof(IAuthenticationAttemptTracker) ||
                                parameter.ParameterType == typeof(IAuthenticationSecurityPolicyProvider))
            .Should().OnlyContain(parameter => !parameter.HasDefaultValue &&
                nullability.Create(parameter).ReadState == NullabilityState.NotNull);
    }

    [Fact]
    public void Production_authentication_registration_resolves_all_security_handlers_with_scope_validation()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddAuthenticationModuleServices(AuthenticationProfilePolicy.Default);
        services.Configure<AuthenticationAttemptKeyOptions>(options =>
        {
            options.Realm = "composition-tests";
            options.CurrentPepper = "composition-authentication-attempt-key-pepper-20260829";
            options.CurrentVersion = "v1";
        });

        services.Replace(ServiceDescriptor.Singleton<IAuthenticationAttemptTracker, InMemoryAuthenticationAttemptTracker>());
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationSecurityPolicyStore>(_ => Mock.Of<IAuthenticationSecurityPolicyStore>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationSecurityPolicyProvider>(_ => new TestSecurityPolicyProvider()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationChallengeStore>(_ => Mock.Of<IAuthenticationChallengeStore>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationUserDirectory>(_ => Mock.Of<IAuthenticationUserDirectory>()));
        services.Replace(ServiceDescriptor.Scoped<IInitialPlatformAdministratorGrant>(_ => Mock.Of<IInitialPlatformAdministratorGrant>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationCodeGenerator>(_ => Mock.Of<IAuthenticationCodeGenerator>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationCodeVerifier>(_ => Mock.Of<IAuthenticationCodeVerifier>()));
        services.Replace(ServiceDescriptor.Scoped<IUserAccountUpdater>(_ => Mock.Of<IUserAccountUpdater>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationSignInService>(_ => Mock.Of<IAuthenticationSignInService>()));
        services.Replace(ServiceDescriptor.Scoped<IAuthenticationRedirectResolver>(_ => Mock.Of<IAuthenticationRedirectResolver>()));
        services.Replace(ServiceDescriptor.Scoped<IEmailAuthenticationCodeSender>(_ => Mock.Of<IEmailAuthenticationCodeSender>()));
        services.Replace(ServiceDescriptor.Scoped<IPhoneAuthenticationCodeSender>(_ => Mock.Of<IPhoneAuthenticationCodeSender>()));
        services.Replace(ServiceDescriptor.Scoped<IWebAuthnAuthenticationService>(_ => Mock.Of<IWebAuthnAuthenticationService>()));
        services.AddSingleton<ISecurityAuditWriter>(Mock.Of<ISecurityAuditWriter>());
        services.AddSingleton<IRequestContext>(new TestRequestContext());

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        foreach (Type handlerType in AuthenticationHandlers().Select(item => (Type)item[0])
            .Concat([typeof(GetSecurityAccessPolicyHandler), typeof(ChangeSecurityAccessPolicyHandler)]))
            scope.ServiceProvider.GetRequiredService(handlerType).Should().NotBeNull();

        scope.ServiceProvider.GetRequiredService<IPasswordRecoveryCodeDispatcher>()
            .Should().BeOfType<PasswordRecoveryCodeDispatcher>();
    }

    [Fact]
    public async Task User_inventory_reads_enabled_methods_from_the_effective_policy_provider()
    {
        ServiceCollection services = new();
        services.AddAuthenticationModuleServices(AuthenticationProfilePolicy.Default);
        SecurityAccessPolicy effective = SecurityAccessPolicy.Default with
        {
            EnabledAuthenticationMechanisms = new HashSet<AuthenticationMethod> { AuthenticationMethod.Password, AuthenticationMethod.Email }
        };
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(value => value.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(effective);
        services.Replace(ServiceDescriptor.Scoped(_ => policy.Object));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        EnabledAuthenticationMethods methods = await scope.ServiceProvider.GetRequiredService<IEnabledAuthenticationMethods>().GetAsync(CancellationToken.None);

        methods.Should().Be(new EnabledAuthenticationMethods(true, false, true, false));
    }

    private sealed class TestSecurityPolicyProvider : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) => Task.FromResult(SecurityAccessPolicy.Default);
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner";
        public string? UserId => "user";
        public string? TenantId => "tenant";
        public string? ResourceId => "authentication";
        public string? TraceId => "trace";
        public string? RequestId => "request";
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId, RequestId = RequestId! };
    }
}
