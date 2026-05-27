using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.EntryService.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_entries_Date",
            table: "entries",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_Published_CreatedAt",
            table: "outbox",
            columns: new[] { "Published", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_entries_Date",
            table: "entries");

        migrationBuilder.DropIndex(
            name: "IX_outbox_Published_CreatedAt",
            table: "outbox");
    }
}
