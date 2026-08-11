using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Api.PatientAgent;

public sealed class ClinicRuntimeToolEndpoint(
    PatientAgentCapabilityRegistry capabilityRegistry,
    PatientAgentToolDispatcher toolDispatcher,
    IPatientAgentOperationsAccessVerifier operationsAccessVerifier,
    IRequestContextWriter requestContextWriter,
    IHttpContextAccessor httpContextAccessor) : IAgentRuntimeToolEndpoint
{
    public IReadOnlyCollection<string> AgentKeys { get; } = ["clinic.patient", "clinic.operations"];

    public async Task<AgentToolResult> ExecuteAsync(AgentRuntimeToolEndpointRequest request, CancellationToken ct)
    {
        PatientAgentContext context = FromExecutionContext(request.ExecutionContext);
        if (context.SubjectKind == "practice")
        {
            string? role = await operationsAccessVerifier.RevalidateAsync(context.PracticeId, context.AuthenticatedUserId, ct);
            if (!PatientAgentOperationsScope.IsAuthorizedRole(role))
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
            additionalInfo: "Signed patient-agent tool callback.");
        IAgentTool? tool = toolDispatcher.CreateTools(context, request.ExecutionContext, capabilityRegistry.GetAvailable(context))
            .FirstOrDefault(candidate => string.Equals(candidate.Definition.Name, request.ToolCall.Name, StringComparison.Ordinal));
        if (tool is null)
        {
            throw new KeyNotFoundException();
        }

        return await tool.ExecuteAsync(request.ToolCall, request.ExecutionContext, ct);
    }

    private static PatientAgentContext FromExecutionContext(AgentExecutionContext context)
    {
        IReadOnlyDictionary<string, string> metadata = context.Metadata ?? throw new InvalidOperationException("Agent execution metadata is required.");
        string Required(string name) => metadata.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Agent execution metadata is missing {name}.");
        string subjectKind = metadata.TryGetValue("subjectKind", out string? supplied) ? supplied : "patient";
        Guid patientId = Guid.Parse(Required("patientId"));
        string role = Required("role");
        if (subjectKind is not ("patient" or "practice") || subjectKind == "patient" && patientId == Guid.Empty || subjectKind == "practice" && (patientId != Guid.Empty || !PatientAgentOperationsScope.IsAuthorizedRole(role)))
        {
            throw new InvalidOperationException("Agent execution subject scope is invalid.");
        }

        return new(Required("practiceId"), patientId, Required("authenticatedUserId"), JsonSerializer.Deserialize<HashSet<string>>(Required("permissions")) ?? [], role, Required("locale"), Required("timeZone"), Required("conversationId"), metadata.TryGetValue("patientNationalId", out string? nationalId) ? nationalId : null, Calendar: metadata.TryGetValue("calendar", out string? calendar) ? calendar : "gregory", SubjectKind: subjectKind);
    }

    private static ClaimsPrincipal CreateOperationsRuntimePrincipal(PatientAgentContext context)
    {
        string role = string.Equals(context.Role, PatientAgentOperationsScope.PlatformAdminRole, StringComparison.Ordinal) ? PatientAgentOperationsScope.PlatformAdminRole : $"{context.PracticeId}:{PatientAgentOperationsScope.TenantOwnerRole}";
        ClaimsIdentity identity = new("vitarish-agent-runtime");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.AuthenticatedUserId));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(identity);
    }
}
