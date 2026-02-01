using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Host.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class EntityBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Owners");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "Tenants",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "Owners",
                newName: "UpdatedBy");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TenantIndicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "TenantIndicators",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TenantIndicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "TenantIndicators",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TenantIndicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Owners",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Owners",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Owners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Owners",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TenantIndicators");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TenantIndicators");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TenantIndicators");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TenantIndicators");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TenantIndicators");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Owners");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Tenants",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Owners",
                newName: "ModifiedBy");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Owners",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Owners",
                type: "datetime2",
                nullable: true);
        }
    }
}
