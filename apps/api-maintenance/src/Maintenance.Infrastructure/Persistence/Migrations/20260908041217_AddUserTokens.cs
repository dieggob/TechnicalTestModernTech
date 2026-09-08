using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purpose = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    token_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    used_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_purpose",
                table: "user_tokens",
                columns: new[] { "user_id", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ux_user_tokens_hash",
                table: "user_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_tokens");
        }
    }
}
