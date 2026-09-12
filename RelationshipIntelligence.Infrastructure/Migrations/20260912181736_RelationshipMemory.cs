using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelationshipMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelationshipEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccursOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RepeatsYearly = table.Column<bool>(type: "bit", nullable: false),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_RelationshipEvents_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipMemoryEntries",
                columns: table => new
                {
                    MemoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Provenance = table.Column<int>(type: "int", nullable: false),
                    SourceMeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceExcerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceMeetingDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CorrectedFromFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrectionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipMemoryEntries", x => x.MemoryEntryId);
                    table.ForeignKey(
                        name: "FK_RelationshipMemoryEntries_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipEvents_ApplicationUserId_OccursOn",
                table: "RelationshipEvents",
                columns: new[] { "ApplicationUserId", "OccursOn" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipEvents_PersonId",
                table: "RelationshipEvents",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipMemoryEntries_ApplicationUserId_PersonId_Status",
                table: "RelationshipMemoryEntries",
                columns: new[] { "ApplicationUserId", "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipMemoryEntries_PersonId",
                table: "RelationshipMemoryEntries",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelationshipEvents");

            migrationBuilder.DropTable(
                name: "RelationshipMemoryEntries");
        }
    }
}
