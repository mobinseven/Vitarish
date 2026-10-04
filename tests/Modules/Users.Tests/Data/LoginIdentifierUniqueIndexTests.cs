using System.Security.Claims;
using FluentAssertions;
using MediatR;
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
using Testcontainers.PostgreSql;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Data;

public sealed class LoginIdentifierUniqueIndexTests
{
    private const string NormalizedPhone = "+989123456789";

    [DockerFact]
    public async Task Unique_violations_for_contact_identifiers_are_classified_but_unrelated_ones_are_not()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);

        await using (UsersDbContext phoneOwner = PostgresTestSupport.CreateContext(connectionString))
        {
            phoneOwner.Users.Add(SeedUser("phone-owner", email: "phone-owner@example.test", phoneNumber: null));
            await phoneOwner.SaveChangesAsync();
        }

        await using (UsersDbContext phoneRacer = PostgresTestSupport.CreateContext(connectionString))
        {
            phoneRacer.Users.Add(SeedUser("phone-racer", email: "phone-racer@example.test", phoneNumber: NormalizedPhone));
            await SetPhoneAsync(phoneRacer, "phone-owner", NormalizedPhone);
            DbUpdateException violation = await RecordUniqueViolationAsync(() => phoneRacer.SaveChangesAsync());

            ContactIdentifierUniqueViolation.Matches(violation).Should().BeTrue();
        }

        await using (UsersDbContext emailOwner = PostgresTestSupport.CreateContext(connectionString))
        {
            emailOwner.Users.Add(SeedUser("email-owner", email: "shared@example.test", phoneNumber: null));
            await emailOwner.SaveChangesAsync();
        }

        await using (UsersDbContext emailRacer = PostgresTestSupport.CreateContext(connectionString))
        {
            emailRacer.Users.Add(SeedUser("email-racer", email: "SHARED@example.test", phoneNumber: null));
            DbUpdateException violation = await RecordUniqueViolationAsync(() => emailRacer.SaveChangesAsync());

            ContactIdentifierUniqueViolation.Matches(violation).Should().BeTrue();
        }

        await using (UsersDbContext primaryKeyRacer = PostgresTestSupport.CreateContext(connectionString))
        {
            primaryKeyRacer.Users.Add(SeedUser("phone-owner", email: "other@example.test", phoneNumber: null));
            DbUpdateException violation = await RecordUniqueViolationAsync(() => primaryKeyRacer.SaveChangesAsync());

            ContactIdentifierUniqueViolation.Matches(violation).Should().BeFalse(
                "only contact-identifier constraints map to the typed conflict");
        }
    }

    [DockerFact]
    public async Task Simultaneous_phone_edits_yield_one_success_and_one_typed_conflict()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.AddRange(
                SeedUser("user-a", email: "a@example.test", phoneNumber: null),
                SeedUser("user-b", email: "b@example.test", phoneNumber: null));
            await seed.SaveChangesAsync();
        }

        UpdateManagedUserCommandHandler first = CreateUpdateHandler(connectionString);
        UpdateManagedUserCommandHandler second = CreateUpdateHandler(connectionString);

        IdentityResult[] results = await Task.WhenAll(
            first.Handle(new UpdateManagedUserCommand("user-a", null, "09123456789", null, null, "rev-user-a"), CancellationToken.None),
            second.Handle(new UpdateManagedUserCommand("user-b", null, "09123456789", null, null, "rev-user-b"), CancellationToken.None));

        results.Count(result => result.Succeeded).Should().Be(1);
        results.Count(result => !result.Succeeded &&
            result.Errors.Any(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse)))
            .Should().Be(1, "the loser must receive the typed conflict, never a server error");

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.CountAsync(user => user.PhoneNumber == NormalizedPhone)).Should().Be(1);
    }

    [DockerFact]
    public async Task Simultaneous_email_edits_yield_one_success_and_one_typed_conflict()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.AddRange(
                SeedUser("user-a", email: "a@example.test", phoneNumber: null),
                SeedUser("user-b", email: "b@example.test", phoneNumber: null));
            await seed.SaveChangesAsync();
        }

        UpdateManagedUserCommandHandler first = CreateUpdateHandler(connectionString);
        UpdateManagedUserCommandHandler second = CreateUpdateHandler(connectionString);

        IdentityResult[] results = await Task.WhenAll(
            first.Handle(new UpdateManagedUserCommand("user-a", "shared@example.test", null, null, null, "rev-user-a"), CancellationToken.None),
            second.Handle(new UpdateManagedUserCommand("user-b", "shared@example.test", null, null, null, "rev-user-b"), CancellationToken.None));

        results.Count(result => result.Succeeded).Should().Be(1);
        results.Count(result => !result.Succeeded &&
            result.Errors.Any(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse)))
            .Should().Be(1, "the loser must receive the typed conflict, never a server error");

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.CountAsync(user => user.NormalizedEmail == "SHARED@EXAMPLE.TEST")).Should().Be(1);
    }

    private static async Task<DbUpdateException> RecordUniqueViolationAsync(Func<Task> saveChanges) =>
        (DbUpdateException)(await Record.ExceptionAsync(saveChanges)
            ?? throw new InvalidOperationException("A unique-constraint violation was expected."));

    private static async Task SetPhoneAsync(UsersDbContext dbContext, string userId, string phoneNumber)
    {
        User user = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId);
        user.PhoneNumber = phoneNumber;
    }

    private static UpdateManagedUserCommandHandler CreateUpdateHandler(string connectionString)
    {
        UsersDbContext dbContext = PostgresTestSupport.CreateContext(connectionString);
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        IRequestContext requestContext = RequestContext();

        return new UpdateManagedUserCommandHandler(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new UsersManagementAudit(auditWriter.Object, requestContext, TimeProvider.System),
            requestContext);
    }

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

    private static ISender CreateSender(UsersDbContext dbContext)
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

    private static User SeedUser(string id, string email, string? phoneNumber) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        PhoneNumber = phoneNumber,
        SecurityStamp = "security-" + id,
        ConcurrencyStamp = "rev-" + id
    };
}
