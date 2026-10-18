using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class ConfirmUserContactCommandHandlerTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task A_verified_email_destination_confirms_the_email_and_is_audited()
    {
        (UserManager<User> users, ConfirmUserContactCommandHandler handler, List<SecurityAuditEvent> events) = await CreateAsync();

        bool confirmed = await handler.Handle(new ConfirmUserContactCommand(UserId, "Owner@Example.test"), CancellationToken.None);

        confirmed.Should().BeTrue();
        User user = (await users.FindByIdAsync(UserId))!;
        user.EmailConfirmed.Should().BeTrue();
        user.PhoneNumberConfirmed.Should().BeFalse();
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Succeeded &&
            audit.RedactedReason == SecurityAuditReasonCatalog.SecurityAttributeChanged);
    }

    [Fact]
    public async Task A_verified_phone_destination_confirms_the_phone()
    {
        (UserManager<User> users, ConfirmUserContactCommandHandler handler, _) = await CreateAsync();

        bool confirmed = await handler.Handle(new ConfirmUserContactCommand(UserId, "+989121234567"), CancellationToken.None);

        confirmed.Should().BeTrue();
        User user = (await users.FindByIdAsync(UserId))!;
        user.PhoneNumberConfirmed.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task A_destination_that_is_not_the_users_contact_confirms_nothing()
    {
        (UserManager<User> users, ConfirmUserContactCommandHandler handler, List<SecurityAuditEvent> events) = await CreateAsync();

        bool confirmed = await handler.Handle(new ConfirmUserContactCommand(UserId, "someone-else@example.test"), CancellationToken.None);

        confirmed.Should().BeFalse();
        User user = (await users.FindByIdAsync(UserId))!;
        user.EmailConfirmed.Should().BeFalse();
        user.PhoneNumberConfirmed.Should().BeFalse();
        events.Should().BeEmpty();
    }

    private static async Task<(UserManager<User>, ConfirmUserContactCommandHandler, List<SecurityAuditEvent>)> CreateAsync()
    {
        UsersDbContext dbContext = new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        UserManager<User> users = new(
            new UserStore<User>(dbContext),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            [new UserValidator<User>()],
            [new PasswordValidator<User>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);
        (await users.CreateAsync(new User { Id = UserId, UserName = "owner@example.test", Email = "owner@example.test", PhoneNumber = "+989121234567" }))
            .Succeeded.Should().BeTrue();

        List<SecurityAuditEvent> events = [];
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((auditEvent, _) => events.Add(auditEvent))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.RequestId).Returns("request-1");
        ConfirmUserContactCommandHandler handler = new(
            users,
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new UsersManagementAudit(auditWriter.Object, requestContext.Object, TimeProvider.System));
        return (users, handler, events);
    }
}
