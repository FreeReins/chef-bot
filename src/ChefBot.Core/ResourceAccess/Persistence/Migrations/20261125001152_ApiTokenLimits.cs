using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefBot.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class ApiTokenLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "ApiTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAt",
                table: "ApiTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReadOnly",
                table: "ApiTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ApiTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "ApiTokens");

            migrationBuilder.DropColumn(
                name: "ReadOnly",
                table: "ApiTokens");
        }
    }
}
