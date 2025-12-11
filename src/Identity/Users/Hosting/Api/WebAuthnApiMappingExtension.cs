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
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Hosting.Api;

/// <summary>
/// Extensions for mapping WebAuthn/passkey endpoints.
/// </summary>
public static class WebAuthnApiMappingExtension
{
    public static IEndpointConventionBuilder MapWebAuthnEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/webauthn")
            .WithTags("WebAuthn");

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
            .Produces(StatusCodes.Status200OK)
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
            .AllowAnonymous();

        _ = group.MapPost("/authenticate/complete", CompleteAuthenticationAsync)
            .WithName("CompleteWebAuthnAuthentication")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Complete WebAuthn authentication")
            .WithDescription("Completes the WebAuthn authentication ceremony and signs in the user.")
            .AllowAnonymous();

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

    private static async Task<IResult> BeginRegistrationAsync(
        ClaimsPrincipal user,
        [FromServices] IFido2 fido2,
        [FromServices] UserManager<User> userManager,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
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

        // Create authenticator selection criteria
        AuthenticatorSelection authenticatorSelection = new()
        {
            RequireResidentKey = false,
            UserVerification = UserVerificationRequirement.Preferred
        };

        // Generate registration options
        CredentialCreateOptions options = fido2.RequestNewCredential(
            user: fido2User,
            excludeCredentials: existingKeys,
            authenticatorSelection: authenticatorSelection,
            attestationPreference: AttestationConveyancePreference.None);

        // Store challenge in session for verification
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Session.SetString($"fido2.attestationOptions.{userId}", options.ToJson());
        }

        return Results.Ok(options);
    }

    private static async Task<IResult> CompleteRegistrationAsync(
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
        string? jsonOptions = httpContext.Session.GetString($"fido2.attestationOptions.{userId}");
        if (string.IsNullOrEmpty(jsonOptions))
        {
            return Results.BadRequest("Registration options not found. Please start registration again.");
        }

        CredentialCreateOptions? options = CredentialCreateOptions.FromJson(jsonOptions);

        try
        {
            // Verify the attestation response using Fido2 3.0.1 API
            var credential = await fido2.MakeNewCredentialAsync(
                attestationResponse,
                options,
                async (args, cancellationToken) =>
                {
                    // Check if credential ID already exists
                    bool exists = await dbContext.WebAuthnCredentials
                        .AnyAsync(c => c.CredentialId == args.CredentialId, cancellationToken);
                    return !exists;
                },
                requestTokenBindingId: null,
                cancellationToken: ct);

            // Store the credential
            // Note: credential.Id should never be null after successful MakeNewCredentialAsync
            // The null-forgiving operator is used because the Fido2 library guarantees this property
            WebAuthnCredential storedCredential = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = credential.Id!,  
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
            httpContext.Session.Remove($"fido2.attestationOptions.{userId}");

            return Results.Ok(new { credentialId = Convert.ToBase64String(storedCredential.CredentialId) });
        }
        catch (Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> BeginAuthenticationAsync(
        [FromBody] BeginAuthenticationRequest request,
        [FromServices] IFido2 fido2,
        [FromServices] UserManager<User> userManager,
        [FromServices] UsersDbContext dbContext,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return Results.BadRequest("Username is required");
        }

        User? user = await userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            // Don't reveal whether the user exists
            return Results.BadRequest("Invalid username or no credentials registered");
        }

        // Get existing credentials for this user
        List<WebAuthnCredential> credentials = await dbContext.WebAuthnCredentials
            .Where(c => c.UserId == user.Id && c.IsActive)
            .ToListAsync(ct);

        if (credentials.Count == 0)
        {
            return Results.BadRequest("No credentials registered for this user");
        }

        List<PublicKeyCredentialDescriptor> allowedCredentials = credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        // Generate authentication options
        AssertionOptions options = fido2.GetAssertionOptions(
            allowedCredentials,
            UserVerificationRequirement.Preferred);

        // Store challenge in session for verification
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Session.SetString($"fido2.assertionOptions.{user.Id}", options.ToJson());
        }

        return Results.Ok(options);
    }

    private static async Task<IResult> CompleteAuthenticationAsync(
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
        string? jsonOptions = httpContext.Session.GetString($"fido2.assertionOptions.{credential.UserId}");
        if (string.IsNullOrEmpty(jsonOptions))
        {
            return Results.BadRequest("Authentication options not found. Please start authentication again.");
        }

        AssertionOptions? options = AssertionOptions.FromJson(jsonOptions);

        try
        {
            // Verify the assertion using Fido2 3.0.1 API
            var result = await fido2.MakeAssertionAsync(
                assertionResponse,
                options,
                credential.PublicKey,
                credential.SignatureCounter,
                async (args, cancellationToken) =>
                {
                    // Verify the credential still exists
                    WebAuthnCredential? storedCred = await dbContext.WebAuthnCredentials
                        .FirstOrDefaultAsync(c => c.CredentialId == args.CredentialId, cancellationToken);
                    return storedCred != null;
                },
                requestTokenBindingId: null,
                cancellationToken: ct);

            // Update the signature counter
            credential.SignatureCounter = result.SignCount;
            credential.LastUsedAt = DateTimeOffset.UtcNow;
            _ = await dbContext.SaveChangesAsync(ct);

            // Sign in the user
            if (credential.User != null)
            {
                await signInManager.SignInAsync(credential.User, isPersistent: true);
            }

            // Clear the session challenge
            httpContext.Session.Remove($"fido2.assertionOptions.{credential.UserId}");

            return Results.Ok(new { success = true });
        }
        catch (Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
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
}

/// <summary>
/// Request model for beginning WebAuthn authentication.
/// </summary>
public record BeginAuthenticationRequest(string Username);

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
