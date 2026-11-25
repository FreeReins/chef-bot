using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefBot.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class ShoppingExtraAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtraAmounts",
                table: "ShoppingItems",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "ExtraAmounts",
                table: "ManualShoppingItems",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraAmounts",
                table: "ShoppingItems");

            migrationBuilder.DropColumn(
                name: "ExtraAmounts",
                table: "ManualShoppingItems");
        }
    }
}
