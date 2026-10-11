using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReelSoon.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class CatalogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatalogSource",
                schema: "app",
                table: "WatchItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "app",
                table: "WatchItems",
                type: "character varying(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overview",
                schema: "app",
                table: "WatchItems",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchItems_OwnerUserId_Kind_CatalogSource_ExternalId",
                schema: "app",
                table: "WatchItems",
                columns: new[] { "OwnerUserId", "Kind", "CatalogSource", "ExternalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchItems_OwnerUserId_Kind_CatalogSource_ExternalId",
                schema: "app",
                table: "WatchItems");

            migrationBuilder.DropColumn(
                name: "CatalogSource",
                schema: "app",
                table: "WatchItems");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "app",
                table: "WatchItems");

            migrationBuilder.DropColumn(
                name: "Overview",
                schema: "app",
                table: "WatchItems");
        }
    }
}
