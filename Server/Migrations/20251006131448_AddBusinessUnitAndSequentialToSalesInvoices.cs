using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessUnitAndSequentialToSalesInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. First ensure we have at least one business unit
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM BusinessUnits)
                BEGIN
                    INSERT INTO BusinessUnits (Code, Name, IsActive, CreatedAt)
                    VALUES ('001', 'Default Business Unit', 1, GETUTCDATE())
                END");

            // 2. Add columns initially as nullable
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns 
                              WHERE object_id = OBJECT_ID(N'[dbo].[SalesInvoices]') 
                              AND name = 'BusinessUnitId')
                BEGIN
                    ALTER TABLE SalesInvoices ADD BusinessUnitId int NULL
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns 
                              WHERE object_id = OBJECT_ID(N'[dbo].[SalesInvoices]') 
                              AND name = 'SequentialNumber')
                BEGIN
                    ALTER TABLE SalesInvoices ADD SequentialNumber int NULL
                END");

            // 3. Update existing records with default business unit
            migrationBuilder.Sql(@"
                UPDATE si
                SET si.BusinessUnitId = (SELECT TOP 1 Id FROM BusinessUnits ORDER BY Id),
                    si.SequentialNumber = 0
                FROM SalesInvoices si
                WHERE si.BusinessUnitId IS NULL");

            // 4. Now make columns non-nullable
            migrationBuilder.Sql(@"
                ALTER TABLE SalesInvoices ALTER COLUMN BusinessUnitId int NOT NULL
                ALTER TABLE SalesInvoices ALTER COLUMN SequentialNumber int NOT NULL");

            // 5. Create InvoiceNumberFormats table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceNumberFormats]') AND type = N'U')
                BEGIN
                    CREATE TABLE [InvoiceNumberFormats] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UseYear] bit NOT NULL DEFAULT(1),
                        [UseSalesCategoryCode] bit NOT NULL DEFAULT(1),
                        [UseBusinessUnitCode] bit NOT NULL DEFAULT(1),
                        [UseSequentialNumber] bit NOT NULL DEFAULT(1),
                        [Separator] nvarchar(max) NOT NULL DEFAULT('-'),
                        [SequentialNumberLength] int NOT NULL DEFAULT(4),
                        [CreatedAt] datetime2 NOT NULL DEFAULT(GETUTCDATE()),
                        [LastModifiedAt] datetime2 NULL,
                        CONSTRAINT [PK_InvoiceNumberFormats] PRIMARY KEY ([Id])
                    )
                END");

            // 6. Create index
            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BusinessUnitId",
                table: "SalesInvoices",
                column: "BusinessUnitId");

            // 7. Add foreign key with RESTRICT instead of CASCADE
            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_BusinessUnits_BusinessUnitId",
                table: "SalesInvoices",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
