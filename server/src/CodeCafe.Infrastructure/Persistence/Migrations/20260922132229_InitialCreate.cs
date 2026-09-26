using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeCafe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    AccessCodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstPageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notebooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notebooks_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirstChildId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextSiblingId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pages_notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_favorites",
                columns: table => new
                {
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_favorites", x => new { x.PageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_page_favorites_pages_PageId",
                        column: x => x.PageId,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_page_favorites_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_shares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_shares_pages_PageId",
                        column: x => x.PageId,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_page_shares_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notebook_favorites_UserId",
                table: "notebook_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notebook_shares_NotebookId_UserId",
                table: "notebook_shares",
                columns: new[] { "NotebookId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notebook_shares_UserId",
                table: "notebook_shares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notebooks_OwnerId",
                table: "notebooks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_notebooks_Slug",
                table: "notebooks",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_favorites_UserId",
                table: "page_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_page_shares_PageId_UserId",
                table: "page_shares",
                columns: new[] { "PageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_shares_UserId",
                table: "page_shares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_pages_NotebookId_ParentId_SortKey",
                table: "pages",
                columns: new[] { "NotebookId", "ParentId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_pages_NotebookId_Slug",
                table: "pages",
                columns: new[] { "NotebookId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedEmail",
                table: "users",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notebook_favorites");

            migrationBuilder.DropTable(
                name: "notebook_shares");

            migrationBuilder.DropTable(
                name: "page_favorites");

            migrationBuilder.DropTable(
                name: "page_shares");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "notebooks");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
