using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <remarks>
    /// These three GIN indexes are built with a plain CREATE INDEX, which holds an ACCESS EXCLUSIVE
    /// lock and blocks writes to the table until the build finishes. That is a write outage on a large
    /// NotebookItems table.
    /// <para>
    /// It is deliberately left as-is: this migration has already been applied to the test and
    /// production databases, so editing it cannot undo the lock that already happened, and EF will not
    /// re-run it. The only databases affected by a change here are fresh ones, where the tables are
    /// empty and the lock is irrelevant.
    /// </para>
    /// <para>
    /// New indexes on tables that can grow must instead be created with
    /// <c>migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS ...", suppressTransaction: true)</c>.
    /// CONCURRENTLY cannot run inside a transaction, so the suppressTransaction flag is required.
    /// MigrationIndexConcurrencyTests enforces this for any migration added after this one.
    /// </para>
    /// </remarks>
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
