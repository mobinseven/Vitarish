using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Exceptions;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Users.Domain.Options;
using Xunit;

namespace Main.Tests;

/// <summary>
/// A passkey ceremony accepts the configured WebAuthn origins plus the origin the host already
/// trusts for the request, so an active clinic's subdomain works without being listed. Fido2 checks
/// the browser's origin before the authenticator data, so a "none" attestation without credential
/// data shows which origins pass: a passing origin then fails on the missing credential data instead.
/// </summary>
public sealed class PasskeyOriginTrustTests
{
    private const string ConfiguredOrigin = "https://app.example.test";
    private const string TrustedTenantHost = "rezaei.clinic.example.test";
    private const string TrustedTenantOrigin = "https://rezaei.clinic.example.test";
    private const string UntrustedTenantOrigin = "https://evil.clinic.example.test";
    private const string RelyingPartyId = "example.test";
    private const string OriginMismatch = "Fully qualified origin";

    [Theory]
    [InlineData(ConfiguredOrigin, ConfiguredOrigin)]
    [InlineData(TrustedTenantOrigin, TrustedTenantOrigin)]
    public async Task Registration_from_a_configured_or_trusted_origin_passes_the_origin_check(string requestOrigin, string clientOrigin)
    {
        using WebApplication app = BuildApp();

        Fido2VerificationException failure = await RegisterAsync(app, requestOrigin, clientOrigin);

        Assert.Equal(Fido2ErrorCode.AttestedCredentialDataFlagNotSet, failure.Code);
    }

    [Theory]
    [InlineData(UntrustedTenantOrigin, UntrustedTenantOrigin)]
    [InlineData(ConfiguredOrigin, TrustedTenantOrigin)]
    [InlineData(TrustedTenantOrigin, UntrustedTenantOrigin)]
    public async Task Registration_from_an_origin_the_request_does_not_trust_is_rejected(string requestOrigin, string clientOrigin)
    {
        using WebApplication app = BuildApp();

        Fido2VerificationException failure = await RegisterAsync(app, requestOrigin, clientOrigin);

        Assert.StartsWith(OriginMismatch, failure.Message);
    }

    [Fact]
    public async Task Unavailable_tenant_origin_trust_leaves_a_clinic_origin_rejected()
    {
        Mock<ITenantOriginTrust> tenantTrust = new();
        _ = tenantTrust
            .Setup(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store unavailable"));
        using WebApplication app = BuildApp(tenantTrust.Object);

        Fido2VerificationException failure = await RegisterAsync(app, TrustedTenantOrigin, TrustedTenantOrigin);

        Assert.StartsWith(OriginMismatch, failure.Message);
    }

    private static WebApplication BuildApp(ITenantOriginTrust? tenantOriginTrust = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration[$"{WebAuthnOptions.SectionName}:{nameof(WebAuthnOptions.ServerDomain)}"] = RelyingPartyId;
        builder.Configuration[$"{WebAuthnOptions.SectionName}:{nameof(WebAuthnOptions.Origins)}:0"] = ConfiguredOrigin;
        _ = builder.Services.AddHttpContextAccessor();
        builder.AddHostCors([ConfiguredOrigin]);
        _ = builder.Services.AddSingleton(tenantOriginTrust ?? CreateTenantTrust());
        builder.AddHostAuthentication(new SecurityConfigurationOptions());
        return builder.Build();
    }

    private static ITenantOriginTrust CreateTenantTrust()
    {
        Mock<ITenantOriginTrust> mock = new();
        _ = mock
            .Setup(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Uri uri, CancellationToken _) => uri.Host == TrustedTenantHost);
        return mock.Object;
    }

    private static async Task<Fido2VerificationException> RegisterAsync(WebApplication app, string requestOrigin, string clientOrigin)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Origin = requestOrigin;
        app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;

        Fido2VerificationException? failure = null;
        ApplicationBuilder appBuilder = new(app.Services);
        _ = appBuilder.UseClientOriginTrust();
        appBuilder.Run(async ctx =>
        {
            IFido2 fido2 = ctx.RequestServices.GetRequiredService<IFido2>();
            CredentialCreateOptions options = fido2.RequestNewCredential(new RequestNewCredentialParams
            {
                User = new Fido2User { Id = [1, 2, 3], Name = "user", DisplayName = "User" }
            });
            failure = await Assert.ThrowsAsync<Fido2VerificationException>(() => fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = AttestationFrom(clientOrigin, options.Challenge),
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = (_, _) => Task.FromResult(true)
            }));
        });

        await appBuilder.Build()(context);
        return failure!;
    }

    private static AuthenticatorAttestationRawResponse AttestationFrom(string origin, byte[] challenge)
    {
        byte[] clientData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            type = "webauthn.create",
            challenge = Base64Url(challenge),
            origin
        }));
        byte[] credentialId = [9, 9, 9, 9];
        return new AuthenticatorAttestationRawResponse
        {
            Id = Base64Url(credentialId),
            RawId = credentialId,
            Type = PublicKeyCredentialType.PublicKey,
            Response = new AuthenticatorAttestationRawResponse.AttestationResponse
            {
                ClientDataJson = clientData,
                AttestationObject = NoneAttestationWithoutCredentialData(),
                Transports = []
            },
            ClientExtensionResults = new AuthenticationExtensionsClientOutputs()
        };
    }

    // CBOR { "fmt": "none", "attStmt": {}, "authData": rpIdHash | user-present flag | zero counter }.
    private static byte[] NoneAttestationWithoutCredentialData()
    {
        byte[] authData = [.. SHA256.HashData(Encoding.UTF8.GetBytes(RelyingPartyId)), 0x01, 0, 0, 0, 0];
        return [0xA3, 0x63, .. "fmt"u8, 0x64, .. "none"u8, 0x67, .. "attStmt"u8, 0xA0, 0x68, .. "authData"u8, 0x58, (byte)authData.Length, .. authData];
    }

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
