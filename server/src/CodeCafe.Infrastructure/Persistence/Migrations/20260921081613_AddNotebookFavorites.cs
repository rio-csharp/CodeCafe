using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotebookFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "notebooks");

            migrationBuilder.CreateTable(
                name: "notebook_favorites",
                columns: table => new
                {
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notebook_favorites", x => new { x.NotebookId, x.UserId });
                    table.ForeignKey(
                        name: "FK_notebook_favorites_notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notebook_favorites_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notebook_favorites_UserId",
                table: "notebook_favorites",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notebook_favorites");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "notebooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
