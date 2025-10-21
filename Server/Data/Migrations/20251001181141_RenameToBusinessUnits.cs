using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameToBusinessUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename table from SaleUnits to BusinessUnits
            migrationBuilder.RenameTable(
                name: "SaleUnits",
                newName: "BusinessUnits",
                schema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert rename if needed
            migrationBuilder.RenameTable(
                name: "BusinessUnits",
                newName: "SaleUnits",
                schema: "dbo");
        }
    }
}
