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
        using var context = UsersDbContextFactory.Create();
        IEntityType entityType = context.Model.FindEntityType(typeof(TemporaryAutoAuthLink))!;

        entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["CodeHash"]) && index.IsUnique)
            .Should()
            .BeTrue();
        entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["UserId"]))
            .Should()
            .BeTrue();
        entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["ExpiresAt"]))
            .Should()
            .BeTrue();
        entityType.GetIndexes().Any(index => index.Properties.Select(p => p.Name).SequenceEqual(["ConsumedAt"]))
            .Should()
            .BeTrue();

        entityType.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(["UserId"]))
            .DeleteBehavior
            .Should()
            .Be(DeleteBehavior.Cascade);
    }
}
