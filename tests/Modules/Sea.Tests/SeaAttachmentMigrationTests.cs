using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Moq;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.Migrations.SeaDb;

namespace Vitarish.Sea.Tests;

public sealed class SeaAttachmentMigrationTests
{
    [Fact]
    public void Historical_migration_remains_the_original_schema_change()
    {
        var migration = new SeaAttachmentKindAndFileName();
        IReadOnlyList<MigrationOperation> operations = InvokeOperations(migration, "Up");

        AddColumnOperation fileName = operations.OfType<AddColumnOperation>().Single(operation => operation.Name == "FileName");
        AddColumnOperation kind = operations.OfType<AddColumnOperation>().Single(operation => operation.Name == "Kind");

        fileName.IsNullable.Should().BeTrue();
        fileName.MaxLength.Should().Be(255);
        kind.IsNullable.Should().BeFalse();
        kind.DefaultValue.Should().Be(0);
        operations.Should().HaveCount(2);
        operations.OfType<SqlOperation>().Should().BeEmpty();
        operations.OfType<AlterColumnOperation>().Should().BeEmpty();
    }

    [Fact]
    public void Forward_repair_normalizes_metadata_without_rewriting_content()
    {
        var migration = new SeaAttachmentLegacyMetadataRepair();
        IReadOnlyList<MigrationOperation> operations = InvokeOperations(migration, "Up");
        string sql = operations.OfType<SqlOperation>().Single().Sql;

        operations.Should().ContainSingle(operation => operation is SqlOperation);
        sql.Should().Contain("lower(coalesce(\"ContentType\", '')) LIKE 'text/%'");
        sql.Should().Contain("CASE WHEN \"TargetKind\" = 0 THEN NULL ELSE \"CandidateFileName\" END");
        sql.Should().Contain("char_length(\"FileName\") <= 255");
        sql.Should().Contain("position(chr(92) in \"FileName\") = 0");
        sql.Should().Contain("attachment-");
        sql.Should().Contain("IS DISTINCT FROM");
        sql.Should().NotContain("\"Content\" =");
        sql.Should().NotContain("data:");

        typeof(SeaAttachmentLegacyMetadataRepair).GetCustomAttribute<DbContextAttribute>()!.ContextType.Should().Be(typeof(Vitarish.Sea.Infrastructure.Data.SeaDbContext));
        typeof(SeaAttachmentLegacyMetadataRepair).GetCustomAttribute<MigrationAttribute>()!.Id.Should().Be("20260811113000_SeaAttachmentLegacyMetadataRepair");
    }

    [Fact]
    public void Forward_repair_does_not_reverse_ambiguous_legacy_metadata()
    {
        var migration = new SeaAttachmentLegacyMetadataRepair();

        InvokeOperations(migration, "Down").Should().BeEmpty();
    }

    [Fact]
    public void Forward_repair_is_discoverable_without_a_model_snapshot_change()
    {
        DbContextOptions<SeaDbContext> options = new DbContextOptionsBuilder<SeaDbContext>()
            .UseNpgsql("Host=localhost;Database=design-time;Username=postgres;Password=postgres")
            .Options;
        using var context = new SeaDbContext(options, [], Mock.Of<IRequestContext>(), new HttpContextAccessor());

        context.GetService<IMigrationsAssembly>().Migrations.Keys.Should().Contain("20260811113000_SeaAttachmentLegacyMetadataRepair");
    }

    [Fact]
    public void Down_removes_kind_and_filename()
    {
        var migration = new SeaAttachmentKindAndFileName();

        string[] removed = [.. InvokeOperations(migration, "Down").OfType<DropColumnOperation>().Select(operation => operation.Name)];

        removed.Should().BeEquivalentTo("Kind", "FileName");
    }

    [Fact]
    public void Migration_target_model_matches_snapshot_for_attachment_fields()
    {
        var migration = new SeaAttachmentKindAndFileName();
        IModel targetModel = migration.TargetModel;
        Type snapshotType = typeof(SeaAttachmentKindAndFileName).Assembly.GetType(
            "Vitarish.Sea.Infrastructure.Migrations.SeaDb.SeaDbContextModelSnapshot",
            throwOnError: true)!;
        var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;

        IProperty targetKind = targetModel.FindEntityType("Vitarish.Sea.Domain.Entities.Attachment")!.FindProperty("Kind")!;
        IProperty snapshotKind = snapshot.Model.FindEntityType("Vitarish.Sea.Domain.Entities.Attachment")!.FindProperty("Kind")!;
        IProperty targetFileName = targetModel.FindEntityType("Vitarish.Sea.Domain.Entities.Attachment")!.FindProperty("FileName")!;
        IProperty snapshotFileName = snapshot.Model.FindEntityType("Vitarish.Sea.Domain.Entities.Attachment")!.FindProperty("FileName")!;

        snapshotKind.ClrType.Should().Be(targetKind.ClrType);
        snapshotKind.IsNullable.Should().Be(targetKind.IsNullable);
        snapshotFileName.ClrType.Should().Be(targetFileName.ClrType);
        snapshotFileName.IsNullable.Should().Be(targetFileName.IsNullable);
        snapshotFileName.GetMaxLength().Should().Be(targetFileName.GetMaxLength()).And.Be(255);
    }

    private static IReadOnlyList<MigrationOperation> InvokeOperations(Migration migration, string methodName)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        MethodInfo method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = method.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
