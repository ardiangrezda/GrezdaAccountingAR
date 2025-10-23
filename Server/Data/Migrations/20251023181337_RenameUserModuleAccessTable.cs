using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserModuleAccessTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.UserModuleAccess', N'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.UserModuleAccess', 'UserModuleAccesses';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.UserModuleAccesses', N'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.UserModuleAccesses', 'UserModuleAccess';
END
");
        }
    }
}
