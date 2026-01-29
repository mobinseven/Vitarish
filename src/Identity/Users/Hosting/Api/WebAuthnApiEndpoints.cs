using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Text.Json;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Users.Hosting.Api;

/// <summary>
/// Extensions for mapping WebAuthn/passkey endpoints using IWebAuthnService.
/// </summary>
public static class WebAuthnApiEndpoints
{
    public static IEndpointConventionBuilder MapWebAuthnApiEndpoints(
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
            .Produces<JsonDocument>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Begin WebAuthn registration")
            .WithDescription("Initiates a WebAuthn registration ceremony and returns options for the client.")
            .RequireAuthorization();

        _ = group.MapPost("/register/complete", CompleteRegistrationAsync)
            .WithName("CompleteWebAuthnRegistration")
            .Produces<RegisterCredentialResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Complete WebAuthn registration")
            .WithDescription("Completes the WebAuthn registration ceremony and stores the credential.")
            .RequireAuthorization();

        // Authentication endpoints
        _ = group.MapPost("/authenticate/begin", BeginAuthenticationAsync)
            .WithName("BeginWebAuthnAuthentication")
            .Produces<JsonDocument>(StatusCodes.Status200OK)
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
            .RequireAuthorization();

        _ = group.MapDelete("/credentials/{credentialId}", DeleteCredentialAsync)
            .WithName("DeleteWebAuthnCredential")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a WebAuthn credential")
            .WithDescription("Removes the specified WebAuthn credential.")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> BeginRegistrationAsync(
        ClaimsPrincipal user,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            string optionsJson = await webAuthnService.BeginRegistrationAsync(userId, ct);
            JsonDocument options = JsonDocument.Parse(optionsJson);
            return Results.Ok(options);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred during registration initialization.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> CompleteRegistrationAsync(
        ClaimsPrincipal user,
        [FromBody] JsonDocument attestationResponse,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            string attestationResponseJson = attestationResponse.RootElement.GetRawText();
            string credentialId = await webAuthnService.CompleteRegistrationAsync(userId, attestationResponseJson, ct);
            return Results.Ok(new RegisterCredentialResponse { CredentialId = credentialId });
        }
        catch (Fido2NetLib.Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred during credential registration.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> BeginAuthenticationAsync(
        [FromBody] BeginAuthenticationRequest request,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return Results.BadRequest("Username is required");
        }

        try
        {
            string optionsJson = await webAuthnService.BeginAuthenticationAsync(request.Username, ct);
            JsonDocument options = JsonDocument.Parse(optionsJson);
            return Results.Ok(options);
        }
        catch (InvalidOperationException)
        {
            // Don't reveal whether the user exists - use same error message
            return Results.BadRequest("Invalid username or no credentials registered");
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred during authentication initialization.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> CompleteAuthenticationAsync(
        [FromBody] JsonDocument assertionResponse,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        try
        {
            string assertionResponseJson = assertionResponse.RootElement.GetRawText();
            bool success = await webAuthnService.CompleteAuthenticationAsync(assertionResponseJson, ct);
            return Results.Ok(new AuthenticationResponse { Success = success });
        }
        catch (Fido2NetLib.Fido2VerificationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred during authentication.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> ListCredentialsAsync(
        ClaimsPrincipal user,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            IEnumerable<WebAuthnCredentialDto> credentials = await webAuthnService.ListCredentialsAsync(userId, ct);
            return Results.Ok(credentials);
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred while retrieving credentials.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteCredentialAsync(
        Guid credentialId,
        ClaimsPrincipal user,
        [FromServices] IWebAuthnService webAuthnService,
        CancellationToken ct)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            bool deleted = await webAuthnService.DeleteCredentialAsync(credentialId, userId, ct);
            if (!deleted)
            {
                return Results.NotFound();
            }
            return Results.NoContent();
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An unexpected error occurred while deleting the credential.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
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
