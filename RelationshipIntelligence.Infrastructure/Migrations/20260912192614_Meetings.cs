using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class Meetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceMeetingId",
                table: "Interactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualOccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Agenda = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RawTranscript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.MeetingId);
                });

            migrationBuilder.CreateTable(
                name: "MeetingBriefs",
                columns: table => new
                {
                    MeetingBriefId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BriefJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingBriefs", x => x.MeetingBriefId);
                    table.ForeignKey(
                        name: "FK_MeetingBriefs_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "MeetingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingFindings",
                columns: table => new
                {
                    MeetingFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappedPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceExcerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcceptedAsEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingFindings", x => x.MeetingFindingId);
                    table.ForeignKey(
                        name: "FK_MeetingFindings_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "MeetingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingFindings_Persons_MappedPersonId",
                        column: x => x.MappedPersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MeetingPersons",
                columns: table => new
                {
                    MeetingPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MappedPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchStatus = table.Column<int>(type: "int", nullable: false),
                    SelectedForLogging = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingPersons", x => x.MeetingPersonId);
                    table.ForeignKey(
                        name: "FK_MeetingPersons_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "MeetingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingPersons_Persons_MappedPersonId",
                        column: x => x.MappedPersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000002-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000003-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000004-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000005-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000006-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000007-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000008-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000009-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a000000a-0000-0000-0000-000000000000"),
                column: "SourceMeetingId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingBriefs_MeetingId",
                table: "MeetingBriefs",
                column: "MeetingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingFindings_MappedPersonId",
                table: "MeetingFindings",
                column: "MappedPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingFindings_MeetingId_MappedPersonId",
                table: "MeetingFindings",
                columns: new[] { "MeetingId", "MappedPersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingPersons_MappedPersonId",
                table: "MeetingPersons",
                column: "MappedPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingPersons_MeetingId",
                table: "MeetingPersons",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_ApplicationUserId_Status",
                table: "Meetings",
                columns: new[] { "ApplicationUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingBriefs");

            migrationBuilder.DropTable(
                name: "MeetingFindings");

            migrationBuilder.DropTable(
                name: "MeetingPersons");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropColumn(
                name: "SourceMeetingId",
                table: "Interactions");
        }
    }
}
