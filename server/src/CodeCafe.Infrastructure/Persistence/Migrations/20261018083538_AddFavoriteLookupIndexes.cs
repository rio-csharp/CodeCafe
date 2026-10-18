using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_page_favorites_UserId",
                table: "page_favorites");

            migrationBuilder.DropIndex(
                name: "IX_notebook_favorites_UserId",
                table: "notebook_favorites");

            migrationBuilder.CreateIndex(
                name: "IX_page_favorites_UserId_PageId",
                table: "page_favorites",
                columns: new[] { "UserId", "PageId" });

            migrationBuilder.CreateIndex(
                name: "IX_notebooks_Visibility_UpdatedAtUtc",
                table: "notebooks",
                columns: new[] { "Visibility", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notebook_favorites_UserId_NotebookId",
                table: "notebook_favorites",
                columns: new[] { "UserId", "NotebookId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_page_favorites_UserId_PageId",
                table: "page_favorites");

            migrationBuilder.DropIndex(
                name: "IX_notebooks_Visibility_UpdatedAtUtc",
                table: "notebooks");

            migrationBuilder.DropIndex(
                name: "IX_notebook_favorites_UserId_NotebookId",
                table: "notebook_favorites");

            migrationBuilder.CreateIndex(
                name: "IX_page_favorites_UserId",
                table: "page_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notebook_favorites_UserId",
                table: "notebook_favorites",
                column: "UserId");
        }
    }
}
