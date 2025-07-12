using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConwaysGameOfLife.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardHistories",
                columns: table => new
                {
                    BoardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Generation = table.Column<int>(type: "INTEGER", nullable: false),
                    AliveCells = table.Column<string>(type: "TEXT", nullable: false),
                    StateHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardHistories", x => new { x.BoardId, x.Generation });
                });

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AliveCells = table.Column<string>(type: "TEXT", nullable: false),
                    Generation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxDimension = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardHistories_BoardId",
                table: "BoardHistories",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardHistories_BoardId_StateHash",
                table: "BoardHistories",
                columns: new[] { "BoardId", "StateHash" });

            migrationBuilder.CreateIndex(
                name: "IX_BoardHistories_StateHash",
                table: "BoardHistories",
                column: "StateHash");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_Id",
                table: "Boards",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardHistories");

            migrationBuilder.DropTable(
                name: "Boards");
        }
    }
}
