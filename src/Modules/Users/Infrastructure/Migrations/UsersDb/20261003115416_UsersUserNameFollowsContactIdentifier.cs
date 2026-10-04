using Microsoft.EntityFrameworkCore.Migrations;
using Vitarish.Users.Infrastructure.Data;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UsersUserNameFollowsContactIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UserNameFollowsContactIdentifierSql.Script);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only data fix: once a stale user name has been moved to
            // the current contact identifier the previous value is unknown.
        }
    }
}
