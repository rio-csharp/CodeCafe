using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotebookTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_Title",
                table: "Notebooks",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_PlainTextContent",
                table: "NotebookItems",
                column: "PlainTextContent")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_Title",
                table: "NotebookItems",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notebooks_Title",
                table: "Notebooks");

            migrationBuilder.DropIndex(
                name: "IX_NotebookItems_PlainTextContent",
                table: "NotebookItems");

            migrationBuilder.DropIndex(
                name: "IX_NotebookItems_Title",
                table: "NotebookItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
