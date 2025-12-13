using System.Security.Claims;
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Hosting.Api;

/// <summary>
/// Extensions for mapping WebAuthn/passkey endpoints.
/// </summary>
public static class WebAuthnApiMappingExtension
{
    public static IEndpointConventionBuilder MapWebAuthnEndpoints(
        this WebApplication app,
        Action<RouteGroupBuilder>? configureFilters = null)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/webauthn")
            .WithTags("WebAuthn");

        configureFilters?.Invoke(group);

        // Registration endpoints
        _ = group.MapPost("/register/begin", BeginRegistrationAsync)
            .WithName("BeginWebAuthnRegistration")
            .Produces<CredentialCreateOptions>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Begin WebAuthn registration")
            .WithDescription("Initiates a WebAuthn registration ceremony and returns options for the client.")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        _ = group.MapPost("/register/complete", CompleteRegistrationAsync)
            .WithName("CompleteWebAuthnRegistration")
            .Produces<RegisterCredentialResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Complete WebAuthn registration")
            .WithDescription("Completes the WebAuthn registration ceremony and stores the credential.")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        // Authentication endpoints
        _ = group.MapPost("/authenticate/begin", BeginAuthenticationAsync)
            .WithName("BeginWebAuthnAuthentication")
            .Produces<AssertionOptions>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Begin WebAuthn authentication")
            .WithDescription("Initiates a WebAuthn authentication ceremony and returns options for the client.")
            .AllowAnonymous()
            .RequireRateLimiting("fixed");

        _ = group.MapPost("/authenticate/complete", CompleteAuthenticationAsync)
            .WithName("CompleteWebAuthnAuthentication")
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Complete WebAuthn authentication")
            .WithDescription("Completes the WebAuthn authentication ceremony and signs in the user.")
            .AllowAnonymous()
            .RequireRateLimiting("fixed");

        // Credential management endpoints
        _ = group.MapGet("/credentials", ListCredentialsAsync)
            .WithName("ListWebAuthnCredentials")
            .Produces<IEnumerable<WebAuthnCredentialDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("List user's WebAuthn credentials")
            .WithDescription("Returns all WebAuthn credentials registered for the authenticated user.")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        _ = group.MapDelete("/credentials/{credentialId}", DeleteCredentialAsync)
            .WithName("DeleteWebAuthnCredential")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a WebAuthn credential")
            .WithDescription("Removes the specified WebAuthn credential.")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        return group;
    }

    internal static async Task<IResult> BeginRegistrationAsync(
        ClaimsPrincipal user,
        [FromServices] IFido2 fido2,
        [FromServices] UserManager<User> userManager,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] WebAuthnOptions webAuthnOptions,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        User? appUser = await userManager.FindByIdAsync(userId);
        if (appUser == null)
        {
            return Results.BadRequest("User not found");
        }

        // Get existing credentials for this user
        List<WebAuthnCredential> existingCredentials = await dbContext.WebAuthnCredentials
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
            RequireResidentKey = ParseResidentKeyRequirement(webAuthnOptions.RequireResidentKey),
            UserVerification = ParseUserVerificationRequirement(webAuthnOptions.UserVerification)
        };

        if (!string.IsNullOrEmpty(webAuthnOptions.AuthenticatorAttachment))
        {
            authenticatorSelection.AuthenticatorAttachment = ParseAuthenticatorAttachment(webAuthnOptions.AuthenticatorAttachment);
        }

        // Generate registration options using configuration
        CredentialCreateOptions options = fido2.RequestNewCredential(
            new RequestNewCredentialParams
            {
                User = fido2User,
                AuthenticatorSelection = authenticatorSelection,
                ExcludeCredentials = existingKeys,
                AttestationPreference = ParseAttestationConveyance(webAuthnOptions.AttestationConveyance)
            });

        // Store challenge in session for verification
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Results.Problem("Unable to access HTTP context for session storage.");
        }
        httpContext.Session.SetString(GetAttestationSessionKey(userId), options.ToJson());

        return Results.Ok(options);
    }

    internal static async Task<IResult> CompleteRegistrationAsync(
        ClaimsPrincipal user,
        [FromBody] AuthenticatorAttestationRawResponse attestationResponse,
        [FromServices] IFido2 fido2,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Results.BadRequest("HTTP context not available");
        }

        // Retrieve the challenge from session
        string? jsonOptions = httpContext.Session.GetString(GetAttestationSessionKey(userId));
        if (string.IsNullOrEmpty(jsonOptions))
        {
            return Results.BadRequest("Registration options not found. Please start registration again.");
        }

        CredentialCreateOptions? options = CredentialCreateOptions.FromJson(jsonOptions);
        if (options == null)
        {
            return Results.BadRequest("Invalid registration options JSON.");
        }

        try
        {
            // Verify the attestation response using Fido2 3.0.1 API
            var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                IsCredentialIdUniqueToUserCallback = async (args, cancellationToken) =>
                    {
                        // Check if credential ID already exists
                        bool exists = await dbContext.WebAuthnCredentials
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

            _ = dbContext.WebAuthnCredentials.Add(storedCredential);
            _ = await dbContext.SaveChangesAsync(ct);

            // Clear the session challenge
            httpContext.Session.Remove(GetAttestationSessionKey(userId));

            return Results.Ok(new RegisterCredentialResponse { CredentialId = Convert.ToBase64String(storedCredential.CredentialId) });
        }
        catch (Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log the exception in production
            return Results.Problem(
                detail: "An unexpected error occurred during credential registration.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    internal static async Task<IResult> BeginAuthenticationAsync(
        [FromBody] BeginAuthenticationRequest request,
        [FromServices] IFido2 fido2,
        [FromServices] UserManager<User> userManager,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] WebAuthnOptions webAuthnOptions,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return Results.BadRequest("Username is required");
        }

        User? user = await userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            // Don't reveal whether the user exists - use same error message
            return Results.BadRequest("Invalid username or no credentials registered");
        }

        // Get existing credentials for this user
        List<WebAuthnCredential> credentials = await dbContext.WebAuthnCredentials
            .Where(c => c.UserId == user.Id && c.IsActive)
            .ToListAsync(ct);

        if (credentials.Count == 0)
        {
            // Use same error message to prevent user enumeration
            return Results.BadRequest("Invalid username or no credentials registered");
        }

        List<PublicKeyCredentialDescriptor> allowedCredentials = credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        // Generate authentication options using configuration
        AssertionOptions options = fido2.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials,
                UserVerification = ParseUserVerificationRequirement(webAuthnOptions.UserVerification)
            }) ?? new AssertionOptions
            {
                AllowCredentials = allowedCredentials,
                UserVerification = ParseUserVerificationRequirement(webAuthnOptions.UserVerification)
            };


        // Store challenge in session for verification
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Results.Problem("Unable to access HTTP context or session. Authentication cannot proceed.");
        }
        httpContext.Session.SetString(GetAssertionSessionKey(user.Id), options.ToJson());

        return Results.Ok(options);
    }

    internal static async Task<IResult> CompleteAuthenticationAsync(
        [FromBody] AuthenticatorAssertionRawResponse assertionResponse,
        [FromServices] IFido2 fido2,
        [FromServices] SignInManager<User> signInManager,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Results.BadRequest("HTTP context not available");
        }

        // Find the credential
        WebAuthnCredential? credential = await dbContext.WebAuthnCredentials
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CredentialId == assertionResponse.RawId, ct);

        if (credential == null || !credential.IsActive)
        {
            return Results.BadRequest("Credential not found or inactive");
        }

        // Retrieve the challenge from session
        string? jsonOptions = httpContext.Session.GetString(GetAssertionSessionKey(credential.UserId));
        if (string.IsNullOrEmpty(jsonOptions))
        {
            return Results.BadRequest("Authentication options not found. Please start authentication again.");
        }

        AssertionOptions? options = AssertionOptions.FromJson(jsonOptions);
        if (options == null)
        {
            return Results.BadRequest("Failed to parse authentication options. Please start authentication again.");
        }

        try
        {
            // Verify the assertion using Fido2 3.0.1 API
            var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, cancellationToken) =>
                    {
                        // Verify the credential still exists
                        WebAuthnCredential? storedCred = await dbContext.WebAuthnCredentials
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
                _ = await dbContext.SaveChangesAsync(ct);

                return Results.BadRequest(new { error = "Potential cloned authenticator detected. Credential has been deactivated for security." });
            }

            // Update the signature counter
            credential.SignatureCounter = result.SignCount;
            credential.LastUsedAt = DateTimeOffset.UtcNow;
            _ = await dbContext.SaveChangesAsync(ct);

            // Sign in the user
            if (credential.User == null)
            {
                return Results.BadRequest("User associated with credential not found.");
            }
            await signInManager.SignInAsync(credential.User, isPersistent: true);

            // Clear the session challenge
            httpContext.Session.Remove(GetAssertionSessionKey(credential.UserId));

            return Results.Ok(new AuthenticationResponse { Success = true });
        }
        catch (Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log the exception in production
            return Results.Problem(
                detail: "An unexpected error occurred during authentication.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> ListCredentialsAsync(
        ClaimsPrincipal user,
        [FromServices] UsersDbContext dbContext,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        List<WebAuthnCredential> credentials = await dbContext.WebAuthnCredentials
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.RegisteredAt)
            .ToListAsync(ct);

        List<WebAuthnCredentialDto> dtos = credentials.Select(c => new WebAuthnCredentialDto
        {
            Id = c.Id,
            FriendlyName = c.FriendlyName,
            RegisteredAt = c.RegisteredAt,
            LastUsedAt = c.LastUsedAt,
            Transports = c.Transports
        }).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> DeleteCredentialAsync(
        Guid credentialId,
        ClaimsPrincipal user,
        [FromServices] UsersDbContext dbContext,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        WebAuthnCredential? credential = await dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId, ct);

        if (credential == null)
        {
            return Results.NotFound();
        }

        _ = dbContext.WebAuthnCredentials.Remove(credential);
        _ = await dbContext.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // Helper methods for parsing configuration strings to enums
    private static bool ParseResidentKeyRequirement(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "required" => true,
            "preferred" => false,
            "discouraged" => false,
            _ => false
        };
    }

    internal static string GetAttestationSessionKey(string userId) => $"fido2.attestationOptions.{userId}";

    internal static string GetAssertionSessionKey(string userId) => $"fido2.assertionOptions.{userId}";

    private static UserVerificationRequirement ParseUserVerificationRequirement(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "required" => UserVerificationRequirement.Required,
            "preferred" => UserVerificationRequirement.Preferred,
            "discouraged" => UserVerificationRequirement.Discouraged,
            _ => UserVerificationRequirement.Preferred
        };
    }

    private static AuthenticatorAttachment? ParseAuthenticatorAttachment(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "platform" => AuthenticatorAttachment.Platform,
            "cross-platform" => AuthenticatorAttachment.CrossPlatform,
            _ => null
        };
    }

    private static AttestationConveyancePreference ParseAttestationConveyance(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "none" => AttestationConveyancePreference.None,
            "indirect" => AttestationConveyancePreference.Indirect,
            "direct" => AttestationConveyancePreference.Direct,
            _ => AttestationConveyancePreference.None
        };
    }
}

/// <summary>
/// Request model for beginning WebAuthn authentication.
/// </summary>
public record BeginAuthenticationRequest(string Username);

/// <summary>
/// Response model for completing WebAuthn registration.
/// </summary>
public record RegisterCredentialResponse
{
    public required string CredentialId { get; init; }
}

/// <summary>
/// Response model for completing WebAuthn authentication.
/// </summary>
public record AuthenticationResponse
{
    public bool Success { get; init; }
}

/// <summary>
/// DTO for WebAuthn credential information.
/// </summary>
public record WebAuthnCredentialDto
{
    public Guid Id { get; init; }
    public string? FriendlyName { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? Transports { get; init; }
}
