using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    make = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    vin = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    license_plate = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    current_mileage = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_current_mileage", "current_mileage >= 0");
                    table.ForeignKey(
                        name: "FK_vehicles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_user_id",
                table: "vehicles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_vehicles_user_vin",
                table: "vehicles",
                columns: new[] { "user_id", "vin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
