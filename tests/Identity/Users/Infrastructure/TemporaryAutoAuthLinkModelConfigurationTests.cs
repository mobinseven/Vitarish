using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Users.Tests.TestHelpers;
using Vitarish.Users.Domain.Entities;

namespace Users.Tests.Infrastructure;

public class TemporaryAutoAuthLinkModelConfigurationTests
{
    [Fact]
    public void Model_ConfiguresTemporaryAutoAuthLinkIndexesAndRelationship()
    {
        using UsersDbContext context = UsersDbContextFactory.Create();
        IEntityType entityType = context.Model.FindEntityType(typeof(TemporaryAutoAuthLink))!;

        _ = entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["CodeHash"]) && index.IsUnique)
            .Should()
            .BeTrue();
        _ = entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["UserId"]))
            .Should()
            .BeTrue();
        _ = entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["ExpiresAt"]))
            .Should()
            .BeTrue();
        _ = entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["ConsumedAt"]))
            .Should()
            .BeTrue();

        _ = entityType.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(["UserId"]))
            .DeleteBehavior
            .Should()
            .Be(DeleteBehavior.Cascade);
    }
}
