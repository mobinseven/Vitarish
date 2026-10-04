using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.Security;

public sealed class AspNetIdentityCodeTests
{
    private const string UserId = "otp-user";

    [Fact]
    public async Task Generated_code_is_six_digits_the_shared_otp_input_accepts()
    {
        await using ServiceProvider services = await CreateServicesAsync();
        using IServiceScope scope = services.CreateScope();
        AspNetIdentityCodeGenerator generator = new(scope.ServiceProvider.GetRequiredService<UserManager<User>>());
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            UserId, "new@example.test", DateTimeOffset.UtcNow);

        string code = await generator.GenerateAsync(UserId, challenge.TokenPurpose, CancellationToken.None);

        code.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task Code_verifies_only_for_the_purpose_it_was_generated_for()
    {
        await using ServiceProvider services = await CreateServicesAsync();
        using IServiceScope scope = services.CreateScope();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        AspNetIdentityCodeGenerator generator = new(userManager);
        AspNetIdentityCodeVerifier verifier = new(userManager);
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            UserId, "new@example.test", DateTimeOffset.UtcNow);
        AuthenticationChallenge otherChallenge = AuthenticationChallengePolicy.Default.CreateEmailPasswordRecoveryChallenge(
            UserId, "otp@example.test", DateTimeOffset.UtcNow);

        string code = await generator.GenerateAsync(UserId, challenge.TokenPurpose, CancellationToken.None);

        (await verifier.VerifyAsync(UserId, challenge.TokenPurpose, code, CancellationToken.None)).Should().BeTrue();
        (await verifier.VerifyAsync(UserId, otherChallenge.TokenPurpose, code, CancellationToken.None)).Should().BeFalse();
        (await verifier.VerifyAsync(UserId, challenge.TokenPurpose, WrongCode(code), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Code_from_one_challenge_does_not_verify_for_a_second_challenge_with_the_same_purpose()
    {
        await using ServiceProvider services = await CreateServicesAsync();
        using IServiceScope scope = services.CreateScope();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        AspNetIdentityCodeGenerator generator = new(userManager);
        AspNetIdentityCodeVerifier verifier = new(userManager);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationChallenge first = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            UserId, "first@example.test", now);
        AuthenticationChallenge second = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            UserId, "second@example.test", now);

        first.TokenPurpose.Should().NotBe(second.TokenPurpose);

        string firstCode = await generator.GenerateAsync(UserId, first.TokenPurpose, CancellationToken.None);
        string secondCode = await generator.GenerateAsync(UserId, second.TokenPurpose, CancellationToken.None);

        (await verifier.VerifyAsync(UserId, first.TokenPurpose, firstCode, CancellationToken.None)).Should().BeTrue();
        (await verifier.VerifyAsync(UserId, second.TokenPurpose, secondCode, CancellationToken.None)).Should().BeTrue();
        (await verifier.VerifyAsync(UserId, second.TokenPurpose, firstCode, CancellationToken.None)).Should().BeFalse();
        (await verifier.VerifyAsync(UserId, first.TokenPurpose, secondCode, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Security_stamp_change_invalidates_an_issued_code()
    {
        await using ServiceProvider services = await CreateServicesAsync();
        using IServiceScope scope = services.CreateScope();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        AspNetIdentityCodeGenerator generator = new(userManager);
        AspNetIdentityCodeVerifier verifier = new(userManager);
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePhoneLoginChallenge(
            UserId, "+15550100", DateTimeOffset.UtcNow);
        string code = await generator.GenerateAsync(UserId, challenge.TokenPurpose, CancellationToken.None);

        User user = (await userManager.FindByIdAsync(UserId))!;
        (await userManager.UpdateSecurityStampAsync(user)).Succeeded.Should().BeTrue();

        (await verifier.VerifyAsync(UserId, challenge.TokenPurpose, code, CancellationToken.None)).Should().BeFalse();
    }

    private static string WrongCode(string code) =>
        string.Concat(code.Select(digit => (char)('0' + ((digit - '0' + 1) % 10))));

    private static async Task<ServiceProvider> CreateServicesAsync()
    {
        ServiceCollection services = new();
        string databaseName = Guid.NewGuid().ToString("N");
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<User>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        dbContext.Users.Add(new User
        {
            Id = UserId,
            UserName = "otp@example.test",
            NormalizedUserName = "OTP@EXAMPLE.TEST",
            Email = "otp@example.test",
            NormalizedEmail = "OTP@EXAMPLE.TEST",
            FirstName = "First",
            LastName = "Last",
            SecurityStamp = "security-otp-user",
            ConcurrencyStamp = "rev-otp-user"
        });
        await dbContext.SaveChangesAsync();
        return provider;
    }
}
