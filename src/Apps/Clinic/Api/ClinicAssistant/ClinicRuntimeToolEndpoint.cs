using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Api.ClinicAssistant;

public sealed class ClinicRuntimeToolEndpoint(
    IClinicWorkspaceGuardedToolResolver toolResolver,
    IClinicWorkspaceOperationsAccessVerifier operationsAccessVerifier,
    IClinicWorkspaceRoutePatientResolver routePatientResolver,
    IClinicWorkspacePatientAccessVerifier patientAccessVerifier,
    IRequestContextWriter requestContextWriter,
    IHttpContextAccessor httpContextAccessor) : IAgentRuntimeToolEndpoint
{
    public IReadOnlyCollection<string> AgentKeys { get; } = ["clinic.assistant.patient", "clinic.assistant.operations", "clinic.patient", "clinic.operations"];

    public async Task<AgentToolResult> ExecuteAsync(AgentRuntimeToolEndpointRequest request, CancellationToken ct)
    {
        ClinicWorkspaceContext context = FromExecutionContext(request.ExecutionContext);
        if (!string.Equals(request.Envelope.AgentKey, request.ExecutionContext.Metadata!["agentKey"], StringComparison.Ordinal) ||
            !string.Equals(request.Envelope.SubjectKind, context.SubjectKind, StringComparison.Ordinal) ||
            !string.Equals(request.Envelope.SubjectId, context.SubjectKind == "practice" ? context.PracticeId : context.PatientId.ToString("D"), StringComparison.Ordinal))
            throw new InvalidOperationException("The callback envelope does not match the Clinic execution scope.");
        if (context.SubjectKind == "patient")
        {
            if (!await routePatientResolver.IsAvailableAsync(context.PracticeId, context.PatientId, ct))
                throw new KeyNotFoundException();

            ClinicWorkspacePatientAccess access = await patientAccessVerifier.RevalidateAsync(
                context.PracticeId, context.AuthenticatedUserId, ct);
            context = context with { Permissions = access.Permissions, Role = access.Role };
        }
        if (context.SubjectKind == "practice")
        {
            string? role = await operationsAccessVerifier.RevalidateAsync(context.PracticeId, context.AuthenticatedUserId, ct);
            if (!ClinicWorkspaceOperationsScope.IsAuthorizedRole(role))
            {
                throw new UnauthorizedAccessException();
            }

            context = context with { Role = role! };
            if (httpContextAccessor.HttpContext is { } http)
            {
                http.User = CreateOperationsRuntimePrincipal(context);
            }
        }
        requestContextWriter.SetContext(
            authority: "vitarish-agent-runtime",
            clientId: "agent-runtime",
            ownerId: context.AuthenticatedUserId,
            tenantId: context.PracticeId,
            userId: context.AuthenticatedUserId,
            resourceId: context.PatientId == Guid.Empty ? null : context.PatientId.ToString("D"),
            traceId: request.ExecutionContext.ExecutionId,
            requestId: request.ToolCall.Id,
            additionalInfo: "Signed clinic-assistant tool callback.");
        ClinicWorkspaceToolResult result = await toolResolver.ResolveAsync(
            new(
                context,
                request.ExecutionContext.ExecutionId,
                request.ExecutionContext.Metadata ?? throw new InvalidOperationException("Agent execution metadata is required."),
                request.ToolCall.Id,
                request.ToolCall.Name,
                request.ToolCall.ArgumentsJson,
                request.Envelope),
            ct);
        return new(result.CallId, result.Output, result.IsError);
    }

    private static ClinicWorkspaceContext FromExecutionContext(AgentExecutionContext context)
    {
        IReadOnlyDictionary<string, string> metadata = context.Metadata ?? throw new InvalidOperationException("Agent execution metadata is required.");
        string Required(string name) => metadata.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Agent execution metadata is missing {name}.");
        string subjectKind = metadata.TryGetValue("subjectKind", out string? supplied) ? supplied : "patient";
        Guid patientId = Guid.Parse(Required("patientId"));
        string role = Required("role");
        if (subjectKind is not ("patient" or "practice") || subjectKind == "patient" && patientId == Guid.Empty || subjectKind == "practice" && (patientId != Guid.Empty || !ClinicWorkspaceOperationsScope.IsAuthorizedRole(role)))
        {
            throw new InvalidOperationException("Agent execution subject scope is invalid.");
        }

        return new(Required("practiceId"), patientId, Required("authenticatedUserId"), JsonSerializer.Deserialize<HashSet<string>>(Required("permissions")) ?? [], role, Required("locale"), Required("timeZone"), Required("conversationId"), metadata.TryGetValue("patientNationalId", out string? nationalId) ? nationalId : null, Calendar: metadata.TryGetValue("calendar", out string? calendar) ? calendar : "gregory", SubjectKind: subjectKind, AgentKey: Required("agentKey"));
    }

    private static ClaimsPrincipal CreateOperationsRuntimePrincipal(ClinicWorkspaceContext context)
    {
        string role = string.Equals(context.Role, ClinicWorkspaceOperationsScope.PlatformAdminRole, StringComparison.Ordinal) ? ClinicWorkspaceOperationsScope.PlatformAdminRole : $"{context.PracticeId}:{ClinicWorkspaceOperationsScope.TenantOwnerRole}";
        ClaimsIdentity identity = new("vitarish-agent-runtime");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.AuthenticatedUserId));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(identity);
    }
}
