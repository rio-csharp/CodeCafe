using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Shared.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260602103000_AddMcpUploadSessions")]
    public partial class AddMcpUploadSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "McpUploadSessions",
                columns: table => new
                {
                    UploadId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MediaType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    BytesReceived = table.Column<int>(type: "integer", nullable: false),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpUploadSessions", x => x.UploadId);
                });

            migrationBuilder.CreateTable(
                name: "McpUploadChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    ContentText = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpUploadChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McpUploadChunks_McpUploadSessions_UploadId",
                        column: x => x.UploadId,
                        principalTable: "McpUploadSessions",
                        principalColumn: "UploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_McpUploadChunks_UploadId_SequenceNumber",
                table: "McpUploadChunks",
                columns: new[] { "UploadId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McpUploadSessions_ActorUserId_UpdatedAtUtc",
                table: "McpUploadSessions",
                columns: new[] { "ActorUserId", "UpdatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "McpUploadChunks");

            migrationBuilder.DropTable(
                name: "McpUploadSessions");
        }
    }
}
