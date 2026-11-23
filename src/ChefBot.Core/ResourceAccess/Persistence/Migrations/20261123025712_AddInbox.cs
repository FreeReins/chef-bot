using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefBot.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxEntries_UserId_CreatedAt",
                table: "InboxEntries",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxEntries_UserId_SourceId",
                table: "InboxEntries",
                columns: new[] { "UserId", "SourceId" },
                unique: true,
                filter: "\"SourceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxEntries");
        }
    }
}
