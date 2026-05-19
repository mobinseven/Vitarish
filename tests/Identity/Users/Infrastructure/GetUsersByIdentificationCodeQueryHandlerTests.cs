using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetUsersByIdentificationCodeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDuplicates_ForSameNormalizedCode()
    {
        await using var db = UsersDbContextFactory.Create();
        db.Users.AddRange(
            new User { Id = "u1", UserName = "u1", IdentificationCode = UserIdentityNormalization.NormalizeIdentificationCode("ab-123") },
            new User { Id = "u2", UserName = "u2", IdentificationCode = UserIdentityNormalization.NormalizeIdentificationCode(" AB123 ") });
        await db.SaveChangesAsync();

        var handler = new GetUsersByIdentificationCodeQueryHandler(db);
        var users = (await handler.Handle(new GetUsersByIdentificationCodeQuery("ab 123"), CancellationToken.None)).ToList();

        users.Should().HaveCount(2);
    }
}
