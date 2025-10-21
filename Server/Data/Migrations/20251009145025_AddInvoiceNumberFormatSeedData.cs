using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNumberFormatSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "InvoiceNumberFormats",
                columns: new[] { "Id", "CreatedAt", "LastModifiedAt", "Separator", "SequentialNumberLength", "UseBusinessUnitCode", "UseSalesCategoryCode", "UseSequentialNumber", "UseYear" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "-", 4, true, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InvoiceNumberFormats",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
