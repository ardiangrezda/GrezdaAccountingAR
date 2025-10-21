using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithVATStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "VATRate",
                table: "Articles");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Articles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description2",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description3",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VATId",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VATTable",
                columns: table => new
                {
                    VATId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VATName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VATTable", x => x.VATId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Barcode",
                table: "Articles",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_VATId",
                table: "Articles",
                column: "VATId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_VAT",
                table: "Articles",
                column: "VATId",
                principalTable: "VATTable",
                principalColumn: "VATId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_VAT",
                table: "Articles");

            migrationBuilder.DropTable(
                name: "VATTable");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Barcode",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_VATId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Description2",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Description3",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "VATId",
                table: "Articles");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Articles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VATRate",
                table: "Articles",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
