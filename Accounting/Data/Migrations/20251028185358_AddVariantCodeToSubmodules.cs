using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantCodeToSubmodules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariantCode",
                table: "Submodules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Submodules_VariantCode",
                table: "Submodules",
                column: "VariantCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submodules_VariantCode",
                table: "Submodules");
            migrationBuilder.DropColumn(
                name: "VariantCode",
                table: "Submodules");
        }
    }
}
