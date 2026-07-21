using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEditProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiEditProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookSlug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEditProposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEditProposals_ActorUserId_ExpiresAtUtc",
                table: "AiEditProposals",
                columns: new[] { "ActorUserId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEditProposals");
        }
    }
}
