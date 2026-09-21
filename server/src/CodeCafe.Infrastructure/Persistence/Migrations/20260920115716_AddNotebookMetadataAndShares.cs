using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotebookMetadataAndShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCodeHash",
                table: "notebooks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "notebooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "notebooks",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "notebook_shares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notebook_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notebook_shares_notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notebook_shares_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notebook_shares_NotebookId_UserId",
                table: "notebook_shares",
                columns: new[] { "NotebookId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notebook_shares_UserId",
                table: "notebook_shares",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notebook_shares");

            migrationBuilder.DropColumn(
                name: "AccessCodeHash",
                table: "notebooks");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "notebooks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "notebooks");
        }
    }
}
