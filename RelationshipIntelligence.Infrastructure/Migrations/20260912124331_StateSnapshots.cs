using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class StateSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelationshipStateSnapshots",
                columns: table => new
                {
                    RelationshipStateSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TakenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TieStrength = table.Column<double>(type: "float", nullable: false),
                    UrgencyScore = table.Column<double>(type: "float", nullable: false),
                    Band = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipStateSnapshots", x => x.RelationshipStateSnapshotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipStateSnapshots_ApplicationUserId_PersonId_TakenAtUtc",
                table: "RelationshipStateSnapshots",
                columns: new[] { "ApplicationUserId", "PersonId", "TakenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelationshipStateSnapshots");
        }
    }
}
