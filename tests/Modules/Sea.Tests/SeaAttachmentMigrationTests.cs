using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Vitarish.Sea.Infrastructure.Migrations.SeaDb;

namespace Vitarish.Sea.Tests;

public sealed class SeaAttachmentMigrationTests
{
    [Fact]
    public void Migration_backfills_legacy_content_before_making_kind_required()
    {
        var migration = new SeaAttachmentKindAndFileName();
        IReadOnlyList<MigrationOperation> operations = InvokeOperations(migration, "Up");
        List<MigrationOperation> ordered = [.. operations];

        AddColumnOperation fileName = operations.OfType<AddColumnOperation>().Single(operation => operation.Name == "FileName");
        AddColumnOperation kind = operations.OfType<AddColumnOperation>().Single(operation => operation.Name == "Kind");
        string sql = operations.OfType<SqlOperation>().Single().Sql;
        AlterColumnOperation requiredKind = operations.OfType<AlterColumnOperation>().Single(operation => operation.Name == "Kind");

        fileName.IsNullable.Should().BeTrue();
        fileName.MaxLength.Should().Be(255);
        kind.IsNullable.Should().BeTrue();
        sql.Should().Contain("^data:");
        sql.Should().Contain("://");
        sql.Should().Contain("[0-9a-f]{8}");
        sql.Should().Contain("ELSE 0");
        sql.Should().NotContain("ContentType");
        requiredKind.IsNullable.Should().BeFalse();
        ordered.IndexOf(kind).Should().BeLessThan(ordered.IndexOf(operations.OfType<SqlOperation>().Single()));
        ordered.IndexOf(operations.OfType<SqlOperation>().Single()).Should().BeLessThan(ordered.IndexOf(requiredKind));
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
