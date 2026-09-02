using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class AuthenticationAttemptKeyFactoryTests
{
    [Fact]
    public void Keys_are_opaque_and_do_not_contain_identifier_text()
    {
        AuthenticationAttemptKeyFactory factory = Create("realm-a", "current-authentication-attempt-pepper-20260829");

        string key = factory.Identifier(AuthenticationMethod.Email, "User@example.test");

        key.Should().StartWith("v1:");
        key.ToUpperInvariant().Should().NotContain("USER");
        key.ToUpperInvariant().Should().NotContain("EXAMPLE");
        key.ToUpperInvariant().Should().NotContain("TEST");
    }

    [Fact]
    public void Different_trusted_realms_cannot_share_keys()
    {
        AuthenticationAttemptKeyFactory first = Create("realm-a", "current-authentication-attempt-pepper-20260829");
        AuthenticationAttemptKeyFactory second = Create("realm-b", "current-authentication-attempt-pepper-20260829");

        first.Identifier(AuthenticationMethod.Phone, "+989121234567")
            .Should().NotBe(second.Identifier(AuthenticationMethod.Phone, "+989121234567"));
    }

    [Fact]
    public void Rotation_exposes_current_and_previous_candidates_until_old_records_expire()
    {
        AuthenticationAttemptKeyFactory factory = new(
            Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "realm-a",
                CurrentPepper = "current-authentication-attempt-pepper-20260829",
                CurrentVersion = "v2",
                PreviousPepper = "previous-authentication-attempt-pepper-20260829",
                PreviousVersion = "v1"
            }));

        IReadOnlyList<string> keys = factory.IdentifierCandidates(AuthenticationMethod.Email, "user@example.test");

        keys.Should().HaveCount(2);
        keys[0].Should().StartWith("v2:");
        keys[1].Should().StartWith("v1:");
    }

    [Fact]
    public void Production_validation_rejects_a_missing_pepper_before_requests_start()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vitarish:Identity:Authority"] = "https://issuer.example"
            })
            .Build();
        AuthenticationAttemptKeyOptionsValidator validator = new(configuration, new TestHostEnvironment(Environments.Production));

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, new AuthenticationAttemptKeyOptions());

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("pepper");
    }

    [Fact]
    public async Task Production_host_startup_rejects_a_missing_pepper()
    {
        using IHost host = BuildProductionHost(new Dictionary<string, string?>
        {
            ["Vitarish:Identity:Authority"] = "https://issuer.example"
        });

        await ((Func<Task>)(() => host.StartAsync(CancellationToken.None))).Should().ThrowAsync<OptionsValidationException>()
            .WithMessage("*pepper*");
    }

    [Fact]
    public void Production_validation_rejects_a_short_pepper_before_requests_start()
    {
        AuthenticationAttemptKeyOptionsValidator validator = new(
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment(Environments.Production));

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, new AuthenticationAttemptKeyOptions
        {
            Realm = "realm-a",
            CurrentPepper = "too-short"
        });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("at least 32 characters");
    }

    [Fact]
    public async Task Production_host_startup_rejects_a_short_pepper()
    {
        using IHost host = BuildProductionHost(new Dictionary<string, string?>
        {
            ["Vitarish:Security:AuthenticationAttempts:Realm"] = "realm-a",
            ["Vitarish:Security:AuthenticationAttempts:CurrentPepper"] = "too-short"
        });

        await ((Func<Task>)(() => host.StartAsync(CancellationToken.None))).Should().ThrowAsync<OptionsValidationException>()
            .WithMessage("*at least 32 characters*");
    }

    [Fact]
    public void Rotation_validation_rejects_a_previous_version_with_a_contract_delimiter()
    {
        AuthenticationAttemptKeyOptionsValidator validator = new(
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment(Environments.Production));

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, new AuthenticationAttemptKeyOptions
        {
            Realm = "realm-a",
            CurrentPepper = "current-authentication-attempt-pepper-20260829",
            PreviousPepper = "previous-authentication-attempt-pepper-20260829",
            PreviousVersion = "v1:invalid"
        });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Previous authentication attempt-key version");
    }

    [Fact]
    public async Task Production_host_startup_rejects_malformed_rotation_configuration()
    {
        using IHost host = BuildProductionHost(new Dictionary<string, string?>
        {
            ["Vitarish:Security:AuthenticationAttempts:Realm"] = "realm-a",
            ["Vitarish:Security:AuthenticationAttempts:CurrentPepper"] = "current-authentication-attempt-pepper-20260829",
            ["Vitarish:Security:AuthenticationAttempts:PreviousPepper"] = "previous-authentication-attempt-pepper-20260829",
            ["Vitarish:Security:AuthenticationAttempts:PreviousVersion"] = "v1:invalid"
        });

        await ((Func<Task>)(() => host.StartAsync(CancellationToken.None))).Should().ThrowAsync<OptionsValidationException>()
            .WithMessage("*Previous authentication attempt-key version*");
    }

    private static AuthenticationAttemptKeyFactory Create(string realm, string pepper) => new(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = realm,
            CurrentPepper = pepper
        }));

    private static IHost BuildProductionHost(Dictionary<string, string?> values)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(values);
        _ = builder.Services.AddOptions<AuthenticationAttemptKeyOptions>()
            .BindConfiguration(AuthenticationAttemptKeyOptions.SectionName)
            .ValidateOnStart();
        _ = builder.Services.AddSingleton<IValidateOptions<AuthenticationAttemptKeyOptions>>(provider =>
            new AuthenticationAttemptKeyOptionsValidator(
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<IHostEnvironment>()));
        return builder.Build();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Authentication.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
