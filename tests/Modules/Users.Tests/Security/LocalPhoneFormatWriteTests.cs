using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using MediatR;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Security;

public sealed class LocalPhoneFormatWriteTests
{
    private const string ManagedUserId = "managed-user";
    private const string SeedRevision = "seed-revision";

    [Fact]
    public async Task Create_with_a_local_format_phone_stores_the_e164_number()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        CreateUserWithPhoneCommandHandler handler = new(CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit());

        User? user = await handler.Handle(new CreateUserWithPhoneCommand("09123456789", "New", "Member"), CancellationToken.None);

        user.Should().NotBeNull();
        user!.PhoneNumber.Should().Be("+989123456789");
        user.UserName.Should().Be("+989123456789");
    }

    [Fact]
    public async Task Set_phone_with_a_local_format_stores_the_e164_number()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        dbContext.Users.Add(new User { Id = "seed-user", UserName = "seed@example.test", Email = "seed@example.test" });
        await dbContext.SaveChangesAsync();
        SetUserPhoneNumberCommandHandler handler = new(CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit());

        IdentityResult result = await handler.Handle(new SetUserPhoneNumberCommand("seed-user", "9123456789"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == "seed-user");
        persisted.PhoneNumber.Should().Be("+989123456789");
    }

    [Fact]
    public async Task Admin_edit_with_persian_digit_phone_stores_the_e164_number()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Id = ManagedUserId,
            UserName = "original@example.test",
            NormalizedUserName = "ORIGINAL@EXAMPLE.TEST",
            Email = "original@example.test",
            NormalizedEmail = "ORIGINAL@EXAMPLE.TEST",
            FirstName = "Original",
            LastName = "User",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = SeedRevision
        });
        await dbContext.SaveChangesAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, null, "۰۹۱۲۳۴۵۶۷۸۹", "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.PhoneNumber.Should().Be("+989123456789");
        persisted.PhoneNumberConfirmed.Should().BeFalse();
        persisted.ConcurrencyStamp.Should().NotBe(SeedRevision);
    }

    [Fact]
    public async Task Admin_edit_with_a_persian_digit_phone_rejects_the_same_number_owned_by_another_account()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new User
            {
                Id = ManagedUserId,
                UserName = "original@example.test",
                NormalizedUserName = "ORIGINAL@EXAMPLE.TEST",
                Email = "original@example.test",
                NormalizedEmail = "ORIGINAL@EXAMPLE.TEST",
                FirstName = "Original",
                LastName = "User",
                SecurityStamp = "security-stamp",
                ConcurrencyStamp = SeedRevision
            },
            new User
            {
                Id = "other-user",
                UserName = "+989123456789",
                NormalizedUserName = "+989123456789",
                PhoneNumber = "+989123456789",
                SecurityStamp = "other-security-stamp",
                ConcurrencyStamp = "other-revision"
            });
        await dbContext.SaveChangesAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, null, "۰۹۱۲۳۴۵۶۷۸۹", "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.PhoneNumber.Should().BeNull();
        persisted.FirstName.Should().Be("Original");
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return writer;
    }

    private static UsersManagementAudit CreateAudit() =>
        new(AuditWriter().Object, RequestContext(), TimeProvider.System);

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
            "test")));
        context.SetupGet(value => value.UserId).Returns("platform-admin");
        context.SetupGet(value => value.RequestId).Returns("request-id");
        context.SetupGet(value => value.TraceId).Returns("trace-id");
        return context.Object;
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));

    private static ISender CreateSender(TestUsersDbContext dbContext)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CheckUserEmailInUseQueryHandler).Assembly));
        _ = services.AddSingleton<UsersDbContext>(dbContext);
        _ = services.AddSingleton<ILookupNormalizer>(new UpperInvariantLookupNormalizer());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static UserManager<User> CreateUserManager(UsersDbContext dbContext) => new(
        new UserStore<User>(dbContext),
        Options.Create(new IdentityOptions()),
        new PasswordHasher<User>(),
        [new UserValidator<User>()],
        [new PasswordValidator<User>()],
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<User>>.Instance);

    private static TestUsersDbContext CreateDbContext()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestUsersDbContext(options);
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
