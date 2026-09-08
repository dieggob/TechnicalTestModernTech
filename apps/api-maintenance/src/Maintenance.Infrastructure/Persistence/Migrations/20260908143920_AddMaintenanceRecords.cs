using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maintenance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    cost_usd = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    date_performed = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    mileage_at_service = table.Column<int>(type: "INTEGER", nullable: false),
                    service_provider = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_records", x => x.id);
                    table.CheckConstraint("ck_maintenance_records_cost_usd", "CAST(cost_usd AS REAL) >= 0");
                    table.CheckConstraint("ck_maintenance_records_mileage", "mileage_at_service >= 0");
                    table.ForeignKey(
                        name: "FK_maintenance_records_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_vehicle_date",
                table: "maintenance_records",
                columns: new[] { "vehicle_id", "date_performed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_records");
        }
    }
}
