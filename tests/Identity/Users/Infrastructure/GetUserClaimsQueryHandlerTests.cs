using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class GetUserClaimsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUserClaimsQueryHandler handler = new(dbContext);

        Func<Task> act = () => handler.Handle(new GetUserClaimsQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Handle_ReturnsUserClaims()
    {
        User user = new() { Id = "user-1", UserName = "user1" };
        IdentityUserClaim<string> claim = new() { UserId = "user-1", ClaimType = "scope", ClaimValue = "read" };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        dbContext.UserClaims.Add(claim);
        await dbContext.SaveChangesAsync();

        GetUserClaimsQueryHandler handler = new(dbContext);

        IEnumerable<Vitarish.Users.Domain.Models.ClaimDto> result = await handler.Handle(new GetUserClaimsQuery("user-1"), CancellationToken.None);

        result.Should().ContainSingle(dto => dto.Type == "scope" && dto.Value == "read");
    }
}
