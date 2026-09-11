using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelationshipState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelationshipStates",
                columns: table => new
                {
                    RelationshipStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TieStrength = table.Column<double>(type: "float", nullable: false),
                    LastContactAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CadenceReferenceDays = table.Column<double>(type: "float", nullable: true),
                    SilenceQuantile = table.Column<double>(type: "float", nullable: true),
                    IsBridge = table.Column<bool>(type: "bit", nullable: false),
                    UrgencyScore = table.Column<double>(type: "float", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipStates", x => x.RelationshipStateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipStates_ApplicationUserId_PersonId",
                table: "RelationshipStates",
                columns: new[] { "ApplicationUserId", "PersonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelationshipStates");
        }
    }
}
