using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Api.Authorization;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Tests.Security;

public sealed class UsersManagementAuthorizationAuditHandlerTests
{
    [Fact]
    public async Task Denied_account_creation_is_audited_with_the_authorization_reason()
    {
        (UsersManagementAuthorizationAuditHandler handler, Mock<ISecurityAuditWriter> writer) = CreateHandler();
        AuthorizationHandlerContext context = CreateAuthorizationContext(
            UsersApiRoutes.AdministrationCreateWithEmailOperation,
            new ClaimsPrincipal(new ClaimsIdentity()),
            includeApplicationAuditMetadata: true);

        await handler.HandleAsync(context);

        SecurityAuditEvent auditEvent = ReadEvents(writer).Single();
        auditEvent.EventName.Should().Be(SecurityAuditEventCatalog.UserCreationFailureEvent);
        auditEvent.Operation.Should().Be(SecurityAuditEventCatalog.UserCreationOperation);
        auditEvent.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.Unauthorized);
        auditEvent.ActorId.Should().Be("blocked-actor");
        auditEvent.SubjectId.Should().BeNull();
    }

    [Fact]
    public async Task Denied_account_edit_is_audited_against_the_target_user()
    {
        (UsersManagementAuthorizationAuditHandler handler, Mock<ISecurityAuditWriter> writer) = CreateHandler();
        AuthorizationHandlerContext context = CreateAuthorizationContext(
            UsersApiRoutes.AdministrationUpdateUserOperation,
            new ClaimsPrincipal(new ClaimsIdentity()),
            includeApplicationAuditMetadata: true,
            targetUserId: "user-42");

        await handler.HandleAsync(context);

        SecurityAuditEvent auditEvent = ReadEvents(writer).Single();
        auditEvent.EventName.Should().Be(SecurityAuditEventCatalog.UserEditFailureEvent);
        auditEvent.Operation.Should().Be(SecurityAuditEventCatalog.UserEditOperation);
        auditEvent.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.Unauthorized);
        auditEvent.SubjectId.Should().Be("user-42");
    }

    [Fact]
    public async Task Authorization_denials_for_unmarked_queries_do_not_write_audit_events()
    {
        (UsersManagementAuthorizationAuditHandler handler, Mock<ISecurityAuditWriter> writer) = CreateHandler();
        AuthorizationHandlerContext context = CreateAuthorizationContext(
            UsersApiRoutes.AdministrationGetUserSecurityOperation,
            new ClaimsPrincipal(new ClaimsIdentity()),
            includeApplicationAuditMetadata: false,
            targetUserId: "user-42");

        await handler.HandleAsync(context);

        writer.Verify(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (UsersManagementAuthorizationAuditHandler Handler, Mock<ISecurityAuditWriter> Writer) CreateHandler()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.UserId).Returns("blocked-actor");
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        requestContext.SetupGet(value => value.TraceId).Returns("trace-id");
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-a");
        return (new UsersManagementAuthorizationAuditHandler(
            new UsersManagementAudit(writer.Object, requestContext.Object, TimeProvider.System)), writer);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        string operationId,
        ClaimsPrincipal user,
        bool includeApplicationAuditMetadata,
        string? targetUserId = null)
    {
        List<object> metadata = [new ModuleEndpointOpenApiConvention(operationId)];
        if (includeApplicationAuditMetadata)
            metadata.Add(new SecurityAuditHandledByApplicationMetadata());

        DefaultHttpContext httpContext = new();
        RouteEndpoint endpoint = new(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(UsersApiRoutes.ById),
            0,
            new EndpointMetadataCollection(metadata),
            operationId);
        httpContext.SetEndpoint(endpoint);
        if (targetUserId is not null)
            httpContext.Request.RouteValues[UsersApiRoutes.UserIdRouteValue] = targetUserId;

        RolesAuthorizationRequirement requirement = new([IdentityDefinitions.PlatformAdmin]);
        return new AuthorizationHandlerContext([requirement], user, httpContext);
    }

    private static SecurityAuditEvent[] ReadEvents(Mock<ISecurityAuditWriter> writer) => writer.Invocations
        .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
        .ToArray();
}
