using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceNumberFormatForThreadSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InvoiceNumberFormats",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessUnitId",
                table: "InvoiceNumberFormats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastUsedSequentialNumber",
                table: "InvoiceNumberFormats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalesCategoryId",
                table: "InvoiceNumberFormats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceNumberFormats_BusinessUnitId_SalesCategoryId",
                table: "InvoiceNumberFormats",
                columns: new[] { "BusinessUnitId", "SalesCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceNumberFormats_SalesCategoryId",
                table: "InvoiceNumberFormats",
                column: "SalesCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceNumberFormats_BusinessUnits_BusinessUnitId",
                table: "InvoiceNumberFormats",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceNumberFormats_SalesCategories_SalesCategoryId",
                table: "InvoiceNumberFormats",
                column: "SalesCategoryId",
                principalTable: "SalesCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceNumberFormats_BusinessUnits_BusinessUnitId",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceNumberFormats_SalesCategories_SalesCategoryId",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceNumberFormats_BusinessUnitId_SalesCategoryId",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceNumberFormats_SalesCategoryId",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropColumn(
                name: "LastUsedSequentialNumber",
                table: "InvoiceNumberFormats");

            migrationBuilder.DropColumn(
                name: "SalesCategoryId",
                table: "InvoiceNumberFormats");

            migrationBuilder.InsertData(
                table: "InvoiceNumberFormats",
                columns: new[] { "Id", "CreatedAt", "LastModifiedAt", "Separator", "SequentialNumberLength", "UseBusinessUnitCode", "UseSalesCategoryCode", "UseSequentialNumber", "UseYear" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "-", 4, true, true, true, true });
        }
    }
}
