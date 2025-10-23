using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserModuleAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserModuleAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    SubmoduleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModuleAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserModuleAccess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserModuleAccess_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserModuleAccess_Submodules_SubmoduleId",
                        column: x => x.SubmoduleId,
                        principalTable: "Submodules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleAccess_ModuleId",
                table: "UserModuleAccess",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleAccess_SubmoduleId",
                table: "UserModuleAccess",
                column: "SubmoduleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleAccess_UserId",
                table: "UserModuleAccess",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserModuleAccess");
        }
    }
}
