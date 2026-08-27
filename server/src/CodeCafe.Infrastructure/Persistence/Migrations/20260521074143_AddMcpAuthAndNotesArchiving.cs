using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpAuthAndNotesArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAtUtc",
                table: "NotebookItems",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                table: "NotebookItems",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "NotebookItems",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "NotebookItems",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "McpToolAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorType = table.Column<string>(
                        type: "character varying(40)",
                        maxLength: 40,
                        nullable: false
                    ),
                    ToolName = table.Column<string>(
                        type: "character varying(160)",
                        maxLength: 160,
                        nullable: false
                    ),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    ResultCode = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80,
                        nullable: false
                    ),
                    ErrorCode = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80,
                        nullable: true
                    ),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpToolAuditEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_NotebookId_IsArchived",
                table: "NotebookItems",
                columns: new[] { "NotebookId", "IsArchived" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_McpToolAuditEntries_ActorUserId_CreatedAtUtc",
                table: "McpToolAuditEntries",
                columns: new[] { "ActorUserId", "CreatedAtUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_McpToolAuditEntries_ToolName_CreatedAtUtc",
                table: "McpToolAuditEntries",
                columns: new[] { "ToolName", "CreatedAtUtc" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "McpToolAuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_NotebookItems_NotebookId_IsArchived",
                table: "NotebookItems"
            );

            migrationBuilder.DropColumn(name: "ArchivedAtUtc", table: "NotebookItems");

            migrationBuilder.DropColumn(name: "ArchivedByUserId", table: "NotebookItems");

            migrationBuilder.DropColumn(name: "IsArchived", table: "NotebookItems");

            migrationBuilder.DropColumn(name: "Revision", table: "NotebookItems");
        }
    }
}
