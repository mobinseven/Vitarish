using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Agents;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Sea.Tests;

public sealed class SeaManagementAccessEvaluatorTests
{
    [Fact]
    public async Task Read_role_can_see_its_project_through_a_readable_event_but_cannot_write_it()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        await using SeaDbContext context = CreateContext();
        SeaManagementAccessEvaluator evaluator = Create(context, userId, roleId, RoleType.Read, eventId, [roleId], []);

        (await evaluator.CanReadProjectAsync(new("tenant", userId.ToString("D")), projectId)).Should().BeTrue();
        (await evaluator.CanWriteEventAsync(new("tenant", userId.ToString("D")), eventId)).Should().BeFalse();
    }

    [Fact]
    public async Task Receipt_mutation_requires_the_current_user_and_read_access()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        await using SeaDbContext context = CreateContext();
        SeaManagementAccessEvaluator evaluator = Create(context, userId, roleId, RoleType.Read, eventId, [roleId], []);
        SeaManagementAgentSubject subject = new("tenant", userId.ToString("D"));

        (await evaluator.IsAuthorizedAsync(subject, "mark_read", JsonSerializer.Serialize(new { userId, id = eventId, kind = "event" }))).Should().BeTrue();
        (await evaluator.IsAuthorizedAsync(subject, "mark_read", JsonSerializer.Serialize(new { userId = Guid.NewGuid(), id = eventId, kind = "event" }))).Should().BeFalse();
    }

    [Fact]
    public async Task Attachment_batch_requires_write_access_to_every_referenced_event()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        await using SeaDbContext context = CreateContext();
        SeaManagementAccessEvaluator evaluator = Create(context, userId, roleId, RoleType.Write, eventId, [roleId], [roleId]);
        SeaManagementAgentSubject subject = new("tenant", userId.ToString("D"));

        (await evaluator.IsAuthorizedAsync(subject, "create_attachment_batch", JsonSerializer.Serialize(new
        {
            items = new[] { new { eventId, content = "data", contentType = "text/plain", size = 4, kind = "Text" } }
        }))).Should().BeTrue();
        (await evaluator.IsAuthorizedAsync(subject, "create_attachment_batch", JsonSerializer.Serialize(new
        {
            items = new[] { new { eventId = Guid.NewGuid(), content = "data", contentType = "text/plain", size = 4, kind = "Text" } }
        }))).Should().BeFalse();
    }

    [Fact]
    public async Task Reuses_role_decisions_for_repeated_event_checks_in_the_same_request_scope()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        Mock<IUsersService> users = new();
        users.Setup(service => service.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IUserRolesService> userRoles = new();
        userRoles.Setup(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<UserRoleReadDto>>.Success([new UserRoleReadDto { RoleId = roleId, Type = RoleType.Read }]));
        Mock<IEventRolesService> eventRoles = new();
        eventRoles.Setup(service => service.GetIdsAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(([roleId], (IReadOnlyList<Guid>)[]));
        Mock<IEventsService> events = new();
        await using SeaDbContext context = CreateContext();
        SeaManagementAccessEvaluator evaluator = new(users.Object, userRoles.Object, eventRoles.Object, events.Object, context);
        SeaManagementAgentSubject subject = new("tenant", userId.ToString("D"));

        (await evaluator.CanReadEventAsync(subject, eventId)).Should().BeTrue();
        (await evaluator.CanReadEventAsync(subject, eventId)).Should().BeTrue();

        users.Verify(service => service.CheckUserRoleAsync(userId.ToString("D"), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        userRoles.Verify(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()), Times.Once);
        eventRoles.Verify(service => service.GetIdsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SeaManagementAccessEvaluator Create(SeaDbContext context, Guid userId, Guid roleId, RoleType roleType, Guid eventId, IReadOnlyList<Guid> readRoles, IReadOnlyList<Guid> writeRoles)
    {
        Mock<IUsersService> users = new();
        users.Setup(service => service.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IUserRolesService> userRoles = new();
        userRoles.Setup(service => service.GetListAsync(It.IsAny<UserRoleDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<UserRoleReadDto>>.Success([new UserRoleReadDto { RoleId = roleId, Type = roleType }]));
        Mock<IEventRolesService> eventRoles = new();
        eventRoles.Setup(service => service.GetIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(([], []));
        eventRoles.Setup(service => service.GetIdsAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((readRoles, writeRoles));
        Mock<IEventsService> events = new();
        events.Setup(service => service.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventReadDto>>.Success([new EventReadDto { Id = eventId }]));
        return new(users.Object, userRoles.Object, eventRoles.Object, events.Object, context);
    }

    private static SeaDbContext CreateContext()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns("tenant");
        DbContextOptions<SeaDbContext> options = new DbContextOptionsBuilder<SeaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new SeaDbContext(options, [], requestContext.Object, new HttpContextAccessor());
    }
}
