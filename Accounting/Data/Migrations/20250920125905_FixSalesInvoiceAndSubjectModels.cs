using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Migrations
{
    /// <inheritdoc />
    public partial class FixSalesInvoiceAndSubjectModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceItems_AspNetUsers_CreatedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceItems_AspNetUsers_LastModifiedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceItems_Subjects_BuyerId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_CreatedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_LastModifiedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "BuyerCode",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "InvoiceExpiryDate",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsPosted",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "SalesInvoiceItems");

            migrationBuilder.RenameColumn(
                name: "BuyerId",
                table: "SalesInvoiceItems",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoiceItems_BuyerId",
                table: "SalesInvoiceItems",
                newName: "IX_SalesInvoiceItems_SalesInvoiceId");

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyerId = table.Column<int>(type: "int", nullable: false),
                    BuyerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuyerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TotalWithoutVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVATAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWithVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_AspNetUsers_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Subjects_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BuyerId",
                table: "SalesInvoices",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CreatedByUserId",
                table: "SalesInvoices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_LastModifiedByUserId",
                table: "SalesInvoices",
                column: "LastModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceItems_SalesInvoices_SalesInvoiceId",
                table: "SalesInvoiceItems",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceItems_SalesInvoices_SalesInvoiceId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "SalesInvoiceItems",
                newName: "BuyerId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoiceItems_SalesInvoiceId",
                table: "SalesInvoiceItems",
                newName: "IX_SalesInvoiceItems_BuyerId");

            migrationBuilder.AddColumn<string>(
                name: "BuyerCode",
                table: "SalesInvoiceItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "SalesInvoiceItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "SalesInvoiceItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SalesInvoiceItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "SalesInvoiceItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "SalesInvoiceItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceExpiryDate",
                table: "SalesInvoiceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "SalesInvoiceItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "SalesInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPosted",
                table: "SalesInvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "SalesInvoiceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedByUserId",
                table: "SalesInvoiceItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedDate",
                table: "SalesInvoiceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_CreatedByUserId",
                table: "SalesInvoiceItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_LastModifiedByUserId",
                table: "SalesInvoiceItems",
                column: "LastModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceItems_AspNetUsers_CreatedByUserId",
                table: "SalesInvoiceItems",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceItems_AspNetUsers_LastModifiedByUserId",
                table: "SalesInvoiceItems",
                column: "LastModifiedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceItems_Subjects_BuyerId",
                table: "SalesInvoiceItems",
                column: "BuyerId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
