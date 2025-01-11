using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TryMudBlazor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialSnippetBlobMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SnippetBlobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnippetBlobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SnippetBlobs_Id",
                table: "SnippetBlobs",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnippetBlobs");
        }
    }
}
