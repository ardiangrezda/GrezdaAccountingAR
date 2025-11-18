using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesReturnFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReturn",
                table: "SalesInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalInvoiceId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalInvoiceNumber",
                table: "SalesInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "SalesInvoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalInvoiceItemId",
                table: "SalesInvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalQuantity",
                table: "SalesInvoiceItems",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_OriginalInvoiceId",
                table: "SalesInvoices",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_OriginalInvoiceItemId",
                table: "SalesInvoiceItems",
                column: "OriginalInvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceItems_SalesInvoiceItems_OriginalInvoiceItemId",
                table: "SalesInvoiceItems",
                column: "OriginalInvoiceItemId",
                principalTable: "SalesInvoiceItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_SalesInvoices_OriginalInvoiceId",
                table: "SalesInvoices",
                column: "OriginalInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceItems_SalesInvoiceItems_OriginalInvoiceItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_SalesInvoices_OriginalInvoiceId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_OriginalInvoiceId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_OriginalInvoiceItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsReturn",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceNumber",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "OriginalQuantity",
                table: "SalesInvoiceItems");
        }
    }
}
