using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Service;

/// <summary>
/// Service for WebAuthn/Passkey authentication operations.
/// </summary>
public class WebAuthnService : IWebAuthnService
{
    private readonly IFido2 _fido2;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly UsersDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WebAuthnOptions _webAuthnOptions;

    public WebAuthnService(
        IFido2 fido2,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        UsersDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        WebAuthnOptions webAuthnOptions)
    {
        _fido2 = fido2;
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _webAuthnOptions = webAuthnOptions;
    }

    public async Task<CredentialCreateOptions> BeginRegistrationAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        User? appUser = await _userManager.FindByIdAsync(userId);
        if (appUser == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Get existing credentials for this user
        List<WebAuthnCredential> existingCredentials = await _dbContext.WebAuthnCredentials
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync(ct);

        List<PublicKeyCredentialDescriptor> existingKeys = existingCredentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        // Create FIDO2 user
        Fido2User fido2User = new()
        {
            Name = appUser.UserName ?? appUser.Email ?? userId,
            Id = Encoding.UTF8.GetBytes(userId),
            DisplayName = $"{appUser.FirstName} {appUser.LastName}".Trim()
        };

        // Create authenticator selection criteria from configuration
        AuthenticatorSelection authenticatorSelection = new()
        {
            RequireResidentKey = ParseResidentKeyRequirement(_webAuthnOptions.RequireResidentKey),
            UserVerification = ParseUserVerificationRequirement(_webAuthnOptions.UserVerification)
        };

        if (!string.IsNullOrEmpty(_webAuthnOptions.AuthenticatorAttachment))
        {
            authenticatorSelection.AuthenticatorAttachment = ParseAuthenticatorAttachment(_webAuthnOptions.AuthenticatorAttachment);
        }

        // Generate registration options using configuration
        CredentialCreateOptions options = _fido2.RequestNewCredential(
            new RequestNewCredentialParams
            {
                User = fido2User,
                AuthenticatorSelection = authenticatorSelection,
                ExcludeCredentials = existingKeys,
                AttestationPreference = ParseAttestationConveyance(_webAuthnOptions.AttestationConveyance)
            });

        // Store challenge in session for verification
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("Unable to access HTTP context for session storage.");
        }
        httpContext.Session.SetString(GetAttestationSessionKey(userId), options.ToJson());

        return options;
    }

    public async Task<string> CompleteRegistrationAsync(string userId, AuthenticatorAttestationRawResponse attestationResponse, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HTTP context not available");
        }

        // Retrieve the challenge from session
        string? jsonOptions = httpContext.Session.GetString(GetAttestationSessionKey(userId));
        if (string.IsNullOrEmpty(jsonOptions))
        {
            throw new InvalidOperationException("Registration options not found. Please start registration again.");
        }

        CredentialCreateOptions? options = CredentialCreateOptions.FromJson(jsonOptions);
        if (options == null)
        {
            throw new InvalidOperationException("Invalid registration options JSON.");
        }

        // Verify the attestation response using Fido2 3.0.1 API
        RegisteredPublicKeyCredential credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            IsCredentialIdUniqueToUserCallback = async (args, cancellationToken) =>
            {
                // Check if credential ID already exists
                bool exists = await _dbContext.WebAuthnCredentials
                    .AnyAsync(c => c.CredentialId == args.CredentialId, cancellationToken);
                return !exists;
            },
            OriginalOptions = options
        },
            cancellationToken: ct);

        WebAuthnCredential storedCredential = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CredentialId = credential.Id,
            PublicKey = credential.PublicKey,
            SignatureCounter = credential.SignCount,
            CredentialType = "public-key",
            AaGuid = credential.AaGuid,
            RegisteredAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _ = _dbContext.WebAuthnCredentials.Add(storedCredential);
        _ = await _dbContext.SaveChangesAsync(ct);

        // Clear the session challenge
        httpContext.Session.Remove(GetAttestationSessionKey(userId));

        return Convert.ToBase64String(storedCredential.CredentialId);
    }

    public async Task<AssertionOptions> BeginAuthenticationAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException("Username is required", nameof(username));
        }

        User? user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            throw new InvalidOperationException("Invalid username or no credentials registered");
        }

        // Get existing credentials for this user
        List<WebAuthnCredential> credentials = await _dbContext.WebAuthnCredentials
            .Where(c => c.UserId == user.Id && c.IsActive)
            .ToListAsync(ct);

        if (credentials.Count == 0)
        {
            throw new InvalidOperationException("Invalid username or no credentials registered");
        }

        List<PublicKeyCredentialDescriptor> allowedCredentials = credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        // Generate authentication options using configuration
        AssertionOptions options = _fido2.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials,
                UserVerification = ParseUserVerificationRequirement(_webAuthnOptions.UserVerification)
            }) ?? new AssertionOptions
            {
                AllowCredentials = allowedCredentials,
                UserVerification = ParseUserVerificationRequirement(_webAuthnOptions.UserVerification)
            };

        // Store challenge in session for verification
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("Unable to access HTTP context or session. Authentication cannot proceed.");
        }
        httpContext.Session.SetString(GetAssertionSessionKey(user.Id), options.ToJson());

        return options;
    }

    public async Task<bool> CompleteAuthenticationAsync(AuthenticatorAssertionRawResponse assertionResponse, CancellationToken ct = default)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HTTP context not available");
        }

        // Find the credential
        WebAuthnCredential? credential = await _dbContext.WebAuthnCredentials
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CredentialId == assertionResponse.RawId, ct);

        if (credential == null || !credential.IsActive)
        {
            throw new InvalidOperationException("Credential not found or inactive");
        }

        // Retrieve the challenge from session
        string? jsonOptions = httpContext.Session.GetString(GetAssertionSessionKey(credential.UserId));
        if (string.IsNullOrEmpty(jsonOptions))
        {
            throw new InvalidOperationException("Authentication options not found. Please start authentication again.");
        }

        AssertionOptions? options = AssertionOptions.FromJson(jsonOptions);
        if (options == null)
        {
            throw new InvalidOperationException("Failed to parse authentication options. Please start authentication again.");
        }

        // Verify the assertion using Fido2 3.0.1 API
        VerifyAssertionResult result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, cancellationToken) =>
            {
                // Verify the credential still exists
                WebAuthnCredential? storedCred = await _dbContext.WebAuthnCredentials
                    .FirstOrDefaultAsync(c => c.CredentialId == args.CredentialId, cancellationToken);
                return storedCred != null;
            },
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey,
            StoredSignatureCounter = credential.SignatureCounter
        },
            cancellationToken: ct);

        // Validate signature counter for clone detection
        if (result.SignCount > 0 && result.SignCount <= credential.SignatureCounter)
        {
            // Potential cloned authenticator detected
            credential.IsActive = false;
            _ = await _dbContext.SaveChangesAsync(ct);

            throw new InvalidOperationException("Potential cloned authenticator detected. Credential has been deactivated for security.");
        }

        // Update the signature counter
        credential.SignatureCounter = result.SignCount;
        credential.LastUsedAt = DateTimeOffset.UtcNow;
        _ = await _dbContext.SaveChangesAsync(ct);

        // Sign in the user
        if (credential.User == null)
        {
            throw new InvalidOperationException("User associated with credential not found.");
        }
        await _signInManager.SignInAsync(credential.User, isPersistent: true);

        // Clear the session challenge
        httpContext.Session.Remove(GetAssertionSessionKey(credential.UserId));

        return true;
    }

    public async Task<IEnumerable<WebAuthnCredentialDto>> ListCredentialsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        List<WebAuthnCredential> credentials = await _dbContext.WebAuthnCredentials
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.RegisteredAt)
            .ToListAsync(ct);

        return credentials.Select(c => new WebAuthnCredentialDto
        {
            Id = c.Id,
            FriendlyName = c.FriendlyName,
            RegisteredAt = c.RegisteredAt,
            LastUsedAt = c.LastUsedAt,
            Transports = c.Transports
        }).ToList();
    }

    public async Task<bool> DeleteCredentialAsync(Guid credentialId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        WebAuthnCredential? credential = await _dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId, ct);

        if (credential == null)
        {
            return false;
        }

        _ = _dbContext.WebAuthnCredentials.Remove(credential);
        _ = await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    // Helper methods for parsing configuration strings to enums
    private static bool ParseResidentKeyRequirement(string value) => value?.ToLowerInvariant() switch
    {
        "required" => true,
        "preferred" => false,
        "discouraged" => false,
        _ => false
    };

    private static string GetAttestationSessionKey(string userId) => $"fido2.attestationOptions.{userId}";

    private static string GetAssertionSessionKey(string userId) => $"fido2.assertionOptions.{userId}";

    private static UserVerificationRequirement ParseUserVerificationRequirement(string value) => value?.ToLowerInvariant() switch
    {
        "required" => UserVerificationRequirement.Required,
        "preferred" => UserVerificationRequirement.Preferred,
        "discouraged" => UserVerificationRequirement.Discouraged,
        _ => UserVerificationRequirement.Preferred
    };

    private static AuthenticatorAttachment? ParseAuthenticatorAttachment(string value) => value?.ToLowerInvariant() switch
    {
        "platform" => AuthenticatorAttachment.Platform,
        "cross-platform" => AuthenticatorAttachment.CrossPlatform,
        _ => null
    };

    private static AttestationConveyancePreference ParseAttestationConveyance(string value) => value?.ToLowerInvariant() switch
    {
        "none" => AttestationConveyancePreference.None,
        "indirect" => AttestationConveyancePreference.Indirect,
        "direct" => AttestationConveyancePreference.Direct,
        _ => AttestationConveyancePreference.None
    };
}
