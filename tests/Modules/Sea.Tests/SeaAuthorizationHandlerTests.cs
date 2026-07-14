using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Api.Authorization;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Tests;

public sealed class SeaAuthorizationHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ReadRoleId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid WriteRoleId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Missing_or_malformed_subject_fails_event_authorization(string? subject)
    {
        SeaEventAuthorizationHandler handler = CreateEventHandler(
            [new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)>
            {
                [Guid.NewGuid()] = ([ReadRoleId], [])
            });
        ClaimsPrincipal principal = Principal(subject);

        bool allowed = await AuthorizeAsync(handler, principal, SeaAuthorizationOperations.Read, new SeaEventResource(Guid.NewGuid()));

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Platform_administrator_and_current_tenant_owner_satisfy_manage()
    {
        var requestContext = RequestContext("tenant-a");
        var handler = new SeaManagementAuthorizationHandler(requestContext.Object);

        bool platform = await AuthorizeAsync(handler, Principal(UserId.ToString(), new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)), SeaAuthorizationOperations.Manage, null);
        bool owner = await AuthorizeAsync(handler, Principal(UserId.ToString(), new Claim(ClaimTypes.Role, $"tenant-a:{IdentityDefinitions.TenantOwner}")), SeaAuthorizationOperations.Manage, null);

        platform.Should().BeTrue();
        owner.Should().BeTrue();
    }

    [Fact]
    public async Task Read_role_grants_read_but_not_write()
    {
        Guid eventId = Guid.NewGuid();
        SeaEventAuthorizationHandler handler = CreateEventHandler(
            [new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [eventId] = ([ReadRoleId], []) });

        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(eventId))).Should().BeTrue();
        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Write, new SeaEventResource(eventId))).Should().BeFalse();
    }

    [Fact]
    public async Task Write_role_grants_read_and_write_while_unrelated_roles_grant_neither()
    {
        Guid eventId = Guid.NewGuid();
        SeaEventAuthorizationHandler writer = CreateEventHandler(
            [new UserRoleReadDto { RoleId = WriteRoleId, Type = RoleType.Write }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [eventId] = ([], [WriteRoleId]) });
        SeaEventAuthorizationHandler unrelated = CreateEventHandler(
            [new UserRoleReadDto { RoleId = Guid.NewGuid(), Type = RoleType.Write }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [eventId] = ([ReadRoleId], [WriteRoleId]) });

        (await AuthorizeAsync(writer, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(eventId))).Should().BeTrue();
        (await AuthorizeAsync(writer, Principal(UserId.ToString()), SeaAuthorizationOperations.Write, new SeaEventResource(eventId))).Should().BeTrue();
        (await AuthorizeAsync(unrelated, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(eventId))).Should().BeFalse();
        (await AuthorizeAsync(unrelated, Principal(UserId.ToString()), SeaAuthorizationOperations.Write, new SeaEventResource(eventId))).Should().BeFalse();
    }

    [Fact]
    public async Task Attachment_uses_only_its_actual_owning_event()
    {
        Guid attachmentId = Guid.NewGuid();
        Guid owningEventId = Guid.NewGuid();
        SeaEventAuthorizationHandler eventHandler = CreateEventHandler(
            [new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [owningEventId] = ([ReadRoleId], []) });
        var attachments = new Mock<IAttachmentsService>();
        attachments.Setup(service => service.GetAsync(attachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<AttachmentReadDto>.Success(new AttachmentReadDto { Id = attachmentId, EventId = owningEventId }));
        var handler = new SeaAttachmentAuthorizationHandler(attachments.Object, eventHandler, Accessor());

        bool allowed = await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaAttachmentResource(attachmentId));

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Project_and_procedure_require_at_least_one_readable_contained_event()
    {
        Guid eventId = Guid.NewGuid();
        SeaEventAuthorizationHandler eventHandler = CreateEventHandler(
            [new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }],
            new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [eventId] = ([ReadRoleId], []) });
        var events = new Mock<IEventsService>();
        events.Setup(service => service.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventReadDto>>.Success([new EventReadDto { Id = eventId }]));
        var project = new SeaProjectAuthorizationHandler(events.Object, eventHandler, RequestContext("tenant-a").Object, Accessor());
        var procedure = new SeaProcedureAuthorizationHandler(events.Object, eventHandler, RequestContext("tenant-a").Object, Accessor());

        (await AuthorizeAsync(project, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaProjectResource(Guid.NewGuid()))).Should().BeTrue();
        (await AuthorizeAsync(procedure, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaProcedureResource(Guid.NewGuid()))).Should().BeTrue();
    }

    [Fact]
    public async Task Hidden_cross_tenant_resources_fail_closed()
    {
        var attachments = new Mock<IAttachmentsService>();
        attachments.Setup(service => service.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<AttachmentReadDto>.Failure(EntityError.NotFound("Not found")));
        var handler = new SeaAttachmentAuthorizationHandler(
            attachments.Object,
            CreateEventHandler([], new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)>()),
            Accessor());

        bool allowed = await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaAttachmentResource(Guid.NewGuid()));

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Request_scoped_role_cache_preserves_decisions_between_resources()
    {
        Guid readableEventId = Guid.NewGuid();
        Guid hiddenEventId = Guid.NewGuid();
        var userRoles = new Mock<IUserRolesService>();
        userRoles.Setup(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<UserRoleReadDto>>.Success([new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }]));
        var eventRoles = new Mock<IEventRolesService>();
        eventRoles.Setup(service => service.GetIdsForEventsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => ids.ToDictionary(
                id => id,
                id => id == readableEventId
                    ? ((IReadOnlyList<Guid>)[ReadRoleId], (IReadOnlyList<Guid>)Array.Empty<Guid>())
                    : ((IReadOnlyList<Guid>)Array.Empty<Guid>(), (IReadOnlyList<Guid>)Array.Empty<Guid>())));
        var handler = new SeaEventAuthorizationHandler(userRoles.Object, eventRoles.Object, RequestContext("tenant-a").Object, Accessor());

        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(readableEventId))).Should().BeTrue();
        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(hiddenEventId))).Should().BeFalse();
        userRoles.Verify(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SeaEventAuthorizationHandler CreateEventHandler(
        IReadOnlyList<UserRoleReadDto> userRoleValues,
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> eventRoleValues)
    {
        var userRoles = new Mock<IUserRolesService>();
        userRoles.Setup(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<UserRoleReadDto>>.Success(userRoleValues));
        var eventRoles = new Mock<IEventRolesService>();
        eventRoles.Setup(service => service.GetIdsForEventsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => ids.ToDictionary(
                id => id,
                id => eventRoleValues.TryGetValue(id, out var roles)
                    ? roles
                    : ((IReadOnlyList<Guid>)Array.Empty<Guid>(), (IReadOnlyList<Guid>)Array.Empty<Guid>())));
        return new SeaEventAuthorizationHandler(userRoles.Object, eventRoles.Object, RequestContext("tenant-a").Object, Accessor());
    }

    private static async Task<bool> AuthorizeAsync(
        IAuthorizationHandler handler,
        ClaimsPrincipal principal,
        IAuthorizationRequirement requirement,
        object? resource)
    {
        var context = new AuthorizationHandlerContext([requirement], principal, resource);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static ClaimsPrincipal Principal(string? subject, params Claim[] claims)
    {
        List<Claim> allClaims = [.. claims];
        if (subject is not null) allClaims.Add(new Claim("sub", subject));
        return new ClaimsPrincipal(new ClaimsIdentity(allClaims, "test", ClaimTypes.Name, ClaimTypes.Role));
    }

    private static Mock<IRequestContext> RequestContext(string tenantId)
    {
        var context = new Mock<IRequestContext>();
        context.SetupGet(value => value.TenantId).Returns(tenantId);
        return context;
    }

    private static IHttpContextAccessor Accessor() => new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
}
