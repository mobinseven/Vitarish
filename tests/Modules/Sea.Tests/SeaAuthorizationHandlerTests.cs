using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Api.Authorization;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Access;
using Vitarish.Sea.Application.Agents;
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
    public async Task Platform_administrator_and_current_tenant_owner_satisfy_management_read()
    {
        var requestContext = RequestContext("tenant-a");
        var handler = new SeaManagementAuthorizationHandler(requestContext.Object);

        bool platform = await AuthorizeAsync(handler, Principal(UserId.ToString(), new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)), SeaAuthorizationOperations.ReadManagement, null);
        bool owner = await AuthorizeAsync(handler, Principal(UserId.ToString(), new Claim(ClaimTypes.Role, $"tenant-a:{IdentityDefinitions.TenantOwner}")), SeaAuthorizationOperations.ReadManagement, null);

        platform.Should().BeTrue();
        owner.Should().BeTrue();
    }

    [Fact]
    public async Task Resource_read_is_not_satisfied_by_management_access()
    {
        var handler = new SeaManagementAuthorizationHandler(RequestContext("tenant-a").Object);

        bool allowed = await AuthorizeAsync(
            handler,
            Principal(UserId.ToString(), new Claim(ClaimTypes.Role, $"tenant-a:{IdentityDefinitions.TenantOwner}")),
            SeaAuthorizationOperations.Read,
            new SeaProjectResource(Guid.NewGuid()));

        allowed.Should().BeFalse();
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
        var resourceAccess = new Mock<ISeaResourceAccess>();
        resourceAccess.Setup(value => value.CanReadAttachmentAsync(attachmentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new SeaAttachmentAuthorizationHandler(resourceAccess.Object, Accessor());

        bool allowed = await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaAttachmentResource(attachmentId));

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Project_and_procedure_require_at_least_one_readable_contained_event()
    {
        var access = new Mock<ISeaManagementAccessEvaluator>();
        access.Setup(value => value.CanReadProjectAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        access.Setup(value => value.CanReadProcedureAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var project = new SeaProjectAuthorizationHandler(access.Object, RequestContext("tenant-a").Object, Accessor());
        var procedure = new SeaProcedureAuthorizationHandler(access.Object, RequestContext("tenant-a").Object, Accessor());

        (await AuthorizeAsync(project, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaProjectResource(Guid.NewGuid()))).Should().BeTrue();
        (await AuthorizeAsync(procedure, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaProcedureResource(Guid.NewGuid()))).Should().BeTrue();
    }

    [Fact]
    public async Task Hidden_cross_tenant_resources_fail_closed()
    {
        var resourceAccess = new Mock<ISeaResourceAccess>();
        resourceAccess.Setup(value => value.CanReadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new SeaAttachmentAuthorizationHandler(
            resourceAccess.Object,
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
        var handler = CreateEventHandler([new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }], new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [readableEventId] = ([ReadRoleId], []), [hiddenEventId] = ([], []) });

        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(readableEventId))).Should().BeTrue();
        (await AuthorizeAsync(handler, Principal(UserId.ToString()), SeaAuthorizationOperations.Read, new SeaEventResource(hiddenEventId))).Should().BeFalse();
    }

    [Fact]
    public async Task Batched_prime_covers_multiple_event_authorization_checks_with_single_lookup()
    {
        Guid eventA = Guid.NewGuid();
        Guid eventB = Guid.NewGuid();
        Guid eventC = Guid.NewGuid();
        var userRoles = new Mock<IUserRolesService>();
        userRoles.Setup(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<UserRoleReadDto>>.Success([new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }]));
        var eventRoles = new Mock<IEventRolesService>();
        eventRoles.Setup(service => service.GetIdsForEventsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => ids.ToDictionary(
                id => id,
                id => id == eventA
                    ? ((IReadOnlyList<Guid>)[ReadRoleId], (IReadOnlyList<Guid>)Array.Empty<Guid>())
                    : ((IReadOnlyList<Guid>)Array.Empty<Guid>(), (IReadOnlyList<Guid>)Array.Empty<Guid>())));
        var handler = CreateEventHandler([new UserRoleReadDto { RoleId = ReadRoleId, Type = RoleType.Read }], new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)> { [eventA] = ([ReadRoleId], []), [eventB] = ([], []), [eventC] = ([], []) });
        ClaimsPrincipal principal = Principal(UserId.ToString());

        await handler.PrimeEventRolesAsync([eventA, eventB, eventC], CancellationToken.None);

        (await handler.IsAuthorizedAsync(principal, SeaAuthorizationOperations.Read, new SeaEventResource(eventA))).Should().BeTrue();
        (await handler.IsAuthorizedAsync(principal, SeaAuthorizationOperations.Read, new SeaEventResource(eventB))).Should().BeFalse();
        (await handler.IsAuthorizedAsync(principal, SeaAuthorizationOperations.Read, new SeaEventResource(eventC))).Should().BeFalse();
    }

    private static SeaEventAuthorizationHandler CreateEventHandler(
        IReadOnlyList<UserRoleReadDto> userRoleValues,
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> eventRoleValues)
    {
        return new SeaEventAuthorizationHandler(Access(userRoleValues, eventRoleValues), RequestContext("tenant-a").Object, Accessor());
    }

    private static ISeaManagementAccessEvaluator Access(
        IReadOnlyList<UserRoleReadDto> assignments,
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> eventRoles)
    {
        HashSet<Guid> read = [.. assignments.Where(role => role.Type == RoleType.Read).Select(role => role.RoleId)];
        HashSet<Guid> write = [.. assignments.Where(role => role.Type == RoleType.Write).Select(role => role.RoleId)];
        Mock<ISeaManagementAccessEvaluator> access = new();
        access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeaManagementAgentSubject _, Guid id, CancellationToken _) => eventRoles.TryGetValue(id, out var roles) && (roles.WriteRoleIds.Any(write.Contains) || roles.ReadRoleIds.Any(role => read.Contains(role) || write.Contains(role))));
        access.Setup(value => value.CanWriteEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeaManagementAgentSubject _, Guid id, CancellationToken _) => eventRoles.TryGetValue(id, out var roles) && roles.WriteRoleIds.Any(write.Contains));
        return access.Object;
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
