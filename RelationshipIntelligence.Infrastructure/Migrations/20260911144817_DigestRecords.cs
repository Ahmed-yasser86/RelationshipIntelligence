using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class DigestRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigestDeliveries",
                columns: table => new
                {
                    DigestDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersonIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestDeliveries", x => x.DigestDeliveryId);
                });

            migrationBuilder.CreateTable(
                name: "DigestMetrics",
                columns: table => new
                {
                    DigestMetricId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Opened = table.Column<bool>(type: "bit", nullable: false),
                    ActionTaken = table.Column<bool>(type: "bit", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestMetrics", x => x.DigestMetricId);
                });

            migrationBuilder.CreateTable(
                name: "DigestPreferences",
                columns: table => new
                {
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Threshold = table.Column<double>(type: "float", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestPreferences", x => x.ApplicationUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigestDeliveries_ApplicationUserId_WeekStartUtc",
                table: "DigestDeliveries",
                columns: new[] { "ApplicationUserId", "WeekStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigestDeliveries");

            migrationBuilder.DropTable(
                name: "DigestMetrics");

            migrationBuilder.DropTable(
                name: "DigestPreferences");
        }
    }
}
