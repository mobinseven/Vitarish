using System.Reflection;
using System.Security.Claims;
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Hosting.Api;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.Hosting;

public class WebAuthnApiMappingExtensionTests
{
    private static readonly MethodInfo BeginRegistration = typeof(WebAuthnApiMappingExtension)
        .GetMethod("BeginRegistrationAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo CompleteRegistration = typeof(WebAuthnApiMappingExtension)
        .GetMethod("CompleteRegistrationAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo BeginAuthentication = typeof(WebAuthnApiMappingExtension)
        .GetMethod("BeginAuthenticationAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo CompleteAuthentication = typeof(WebAuthnApiMappingExtension)
        .GetMethod("CompleteAuthenticationAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public async Task BeginAndCompleteRegistrationFlow_PersistsCredentialAndClearsSession()
    {
        using UsersDbContext dbContext = CreateDbContext();
        User user = new() { Id = "user-1", UserName = "user1", FirstName = "Test", LastName = "User" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<UserManager<User>> userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        Mock<IFido2> fido2 = new();
        byte[] credentialId = [1, 2, 3, 4];
        byte[] publicKey = [9, 9, 9, 9];
        CredentialCreateOptions createOptions = new()
        {
            Rp = new PublicKeyCredentialRpEntity("localhost", "localhost", "localhost"),
            User = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id),
                Name = user.UserName!,
                DisplayName = "Test User"
            },
            Challenge = [5, 6, 7],
            Timeout = 60000,
            Attestation = AttestationConveyancePreference.None,
            AuthenticatorSelection = new AuthenticatorSelection(),
            Status = "ok"
        };

        fido2.Setup(f => f.RequestNewCredential(
                It.IsAny<Fido2User>(),
                It.IsAny<List<PublicKeyCredentialDescriptor>>(),
                It.IsAny<AuthenticatorSelection>(),
                It.IsAny<AttestationConveyancePreference>(),
                It.IsAny<AuthenticationExtensionsClientInputs>()))
            .Returns(createOptions);

        AttestationVerificationSuccess attestation = new()
        {
            CredentialId = credentialId,
            PublicKey = publicKey,
            Counter = 1,
            Aaguid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Status = "ok"
        };

        Fido2.CredentialMakeResult makeResult = new("ok", string.Empty, attestation);

        fido2.Setup(f => f.MakeNewCredentialAsync(
                It.IsAny<AuthenticatorAttestationRawResponse>(),
                It.IsAny<CredentialCreateOptions>(),
                It.IsAny<IsCredentialIdUniqueToUserAsyncDelegate>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(makeResult);

        TestSession session = new();
        DefaultHttpContext httpContext = CreateHttpContext(session);
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        WebAuthnOptions options = new()
        {
            ServerDomain = "localhost",
            Origins = ["https://localhost"]
        };
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        ], "test"));

        string sessionKey = $"fido2.attestationOptions.{user.Id}";

        IResult beginResult = await (Task<IResult>)BeginRegistration.Invoke(null, new object[]
        {
            principal,
            fido2.Object,
            userManager.Object,
            dbContext,
            accessor.Object,
            options,
            CancellationToken.None
        })!;

        beginResult.Should().BeOfType<Ok<CredentialCreateOptions>>();
        session.TryGetValue(sessionKey, out _).Should().BeTrue();

        AuthenticatorAttestationRawResponse attestationResponse = new() { RawId = credentialId };

        IResult completeResult = await (Task<IResult>)CompleteRegistration.Invoke(null, new object[]
        {
            principal,
            attestationResponse,
            fido2.Object,
            dbContext,
            accessor.Object,
            CancellationToken.None
        })!;

        completeResult.Should().BeOfType<Ok<RegisterCredentialResponse>>();

        WebAuthnCredential stored = dbContext.WebAuthnCredentials.Single();
        stored.CredentialId.Should().Equal(credentialId);
        stored.PublicKey.Should().Equal(publicKey);
        stored.SignatureCounter.Should().Be(1);
        session.TryGetValue(sessionKey, out _).Should().BeFalse();
    }

    [Fact]
    public async Task BeginAndCompleteAuthenticationFlow_UpdatesCounterAndSignsIn()
    {
        using UsersDbContext dbContext = CreateDbContext();
        byte[] credentialId = [4, 3, 2, 1];
        byte[] publicKey = [8, 8, 8, 8];

        User user = new() { Id = "user-2", UserName = "login-user" };
        dbContext.Users.Add(user);
        dbContext.WebAuthnCredentials.Add(new WebAuthnCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CredentialId = credentialId,
            PublicKey = publicKey,
            SignatureCounter = 1,
            CredentialType = "public-key",
            AaGuid = Guid.NewGuid(),
            RegisteredAt = DateTimeOffset.UtcNow,
            IsActive = true,
            User = user
        });
        await dbContext.SaveChangesAsync();

        Mock<UserManager<User>> userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);

        TestSession session = new();
        DefaultHttpContext httpContext = CreateHttpContext(session);
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        Mock<SignInManager<User>> signInManager = CreateSignInManagerMock(userManager.Object, httpContext);

        Mock<IFido2> fido2 = new();
        AssertionOptions assertionOptions = new()
        {
            Challenge = [7, 7, 7],
            RpId = "localhost",
            AllowCredentials = [new PublicKeyCredentialDescriptor(credentialId)],
            UserVerification = UserVerificationRequirement.Preferred,
            Status = "ok"
        };

        fido2.Setup(f => f.GetAssertionOptions(
                It.IsAny<IEnumerable<PublicKeyCredentialDescriptor>>(),
                It.IsAny<UserVerificationRequirement>(),
                It.IsAny<AuthenticationExtensionsClientInputs>()))
            .Returns(assertionOptions);

        fido2.Setup(f => f.MakeAssertionAsync(
                It.IsAny<AuthenticatorAssertionRawResponse>(),
                It.IsAny<AssertionOptions>(),
                It.Is<byte[]>(pk => pk.SequenceEqual(publicKey)),
                1u,
                It.IsAny<IsUserHandleOwnerOfCredentialIdAsync>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssertionVerificationResult
            {
                CredentialId = credentialId,
                Counter = 2,
                Status = "ok"
            });

        WebAuthnOptions options = new()
        {
            ServerDomain = "localhost",
            Origins = ["https://localhost"],
            UserVerification = "preferred"
        };

        IResult beginResult = await (Task<IResult>)BeginAuthentication.Invoke(null, new object[]
        {
            new BeginAuthenticationRequest(user.UserName!),
            fido2.Object,
            userManager.Object,
            dbContext,
            accessor.Object,
            options,
            CancellationToken.None
        })!;

        beginResult.Should().BeOfType<Ok<AssertionOptions>>();
        session.TryGetValue($"fido2.assertionOptions.{user.Id}", out _).Should().BeTrue();

        AuthenticatorAssertionRawResponse assertionResponse = new() { RawId = credentialId };

        IResult completeResult = await (Task<IResult>)CompleteAuthentication.Invoke(null, new object[]
        {
            assertionResponse,
            fido2.Object,
            signInManager.Object,
            dbContext,
            accessor.Object,
            CancellationToken.None
        })!;

        completeResult.Should().BeOfType<Ok<AuthenticationResponse>>();

        WebAuthnCredential credential = dbContext.WebAuthnCredentials.Single();
        credential.SignatureCounter.Should().Be(2);
        credential.LastUsedAt.Should().NotBeNull();
        session.TryGetValue($"fido2.assertionOptions.{user.Id}", out _).Should().BeFalse();
        signInManager.Verify(s => s.SignInAsync(user, true, null), Times.Once);
    }

    private static UsersDbContext CreateDbContext()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UsersDbContext(options);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        Mock<IUserStore<User>> store = new();
        Mock<IOptions<IdentityOptions>> options = new();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<UserManager<User>>(
            store.Object,
            options.Object,
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }

    private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager, DefaultHttpContext httpContext)
    {
        Mock<IHttpContextAccessor> contextAccessor = new();
        contextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        Mock<IUserClaimsPrincipalFactory<User>> claimsFactory = new();
        claimsFactory.Setup(f => f.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, u.Id)
            ], "mock")));

        Mock<IOptions<IdentityOptions>> options = new();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<SignInManager<User>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());
    }

    private static DefaultHttpContext CreateHttpContext(TestSession session)
    {
        DefaultHttpContext context = new();
        context.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
        return context;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> store = new();

        public IEnumerable<string> Keys => store.Keys;
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public void Clear() => store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => store.Remove(key);
        public void Set(string key, byte[] value) => store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => store.TryGetValue(key, out value!);
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }
}
