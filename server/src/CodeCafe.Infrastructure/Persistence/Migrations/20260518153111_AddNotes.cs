using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(
                        type: "character varying(160)",
                        maxLength: 160,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "character varying(180)",
                        maxLength: 180,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    Visibility = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    PublishedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notebooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notebooks_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "NotebookItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    Title = table.Column<string>(
                        type: "character varying(160)",
                        maxLength: 160,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "character varying(180)",
                        maxLength: 180,
                        nullable: false
                    ),
                    Path = table.Column<string>(
                        type: "character varying(1024)",
                        maxLength: 1024,
                        nullable: false
                    ),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentFormat = table.Column<string>(
                        type: "character varying(40)",
                        maxLength: 40,
                        nullable: true
                    ),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: true),
                    PlainTextContent = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_NotebookItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotebookItems_NotebookItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "NotebookItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_NotebookItems_Notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "Notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_NotebookId_ParentId_SortOrder",
                table: "NotebookItems",
                columns: new[] { "NotebookId", "ParentId", "SortOrder" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_NotebookId_Path",
                table: "NotebookItems",
                columns: new[] { "NotebookId", "Path" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotebookItems_ParentId",
                table: "NotebookItems",
                column: "ParentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_OwnerId",
                table: "Notebooks",
                column: "OwnerId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_Slug",
                table: "Notebooks",
                column: "Slug",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_Visibility_IsPublished",
                table: "Notebooks",
                columns: new[] { "Visibility", "IsPublished" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NotebookItems");

            migrationBuilder.DropTable(name: "Notebooks");
        }
    }
}
