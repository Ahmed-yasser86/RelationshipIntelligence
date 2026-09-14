using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class IngestionAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngestionBatches",
                columns: table => new
                {
                    IngestionBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceMeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceTextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsNoOp = table.Column<bool>(type: "bit", nullable: false),
                    NoOpReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionBatches", x => x.IngestionBatchId);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipPreferences",
                columns: table => new
                {
                    RelationshipPreferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesiredCadenceDays = table.Column<int>(type: "int", nullable: true),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    KeepInTouchIntentionally = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeFromSuggestions = table.Column<bool>(type: "bit", nullable: false),
                    ReminderEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderIntervalDays = table.Column<int>(type: "int", nullable: true),
                    ReminderStrict = table.Column<bool>(type: "bit", nullable: false),
                    SnoozedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSkippedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipPreferences", x => x.RelationshipPreferenceId);
                    table.ForeignKey(
                        name: "FK_RelationshipPreferences_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestionFindings",
                columns: table => new
                {
                    IngestionFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngestionBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectIsNew = table.Column<bool>(type: "bit", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ObjectPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ObjectOrg = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelationKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TargetField = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MemoryKind = table.Column<int>(type: "int", nullable: true),
                    EventKind = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceExcerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    UncertaintyReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConflictType = table.Column<int>(type: "int", nullable: false),
                    ExistingValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProposalAction = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubjectEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedMemoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionFindings", x => x.IngestionFindingId);
                    table.ForeignKey(
                        name: "FK_IngestionFindings_IngestionBatches_IngestionBatchId",
                        column: x => x.IngestionBatchId,
                        principalTable: "IngestionBatches",
                        principalColumn: "IngestionBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngestionFindings_Persons_SubjectPersonId",
                        column: x => x.SubjectPersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionBatches_ApplicationUserId_SourceTextHash",
                table: "IngestionBatches",
                columns: new[] { "ApplicationUserId", "SourceTextHash" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionBatches_ApplicationUserId_Status",
                table: "IngestionBatches",
                columns: new[] { "ApplicationUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFindings_ApplicationUserId_Status",
                table: "IngestionFindings",
                columns: new[] { "ApplicationUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFindings_IngestionBatchId_SubjectPersonId",
                table: "IngestionFindings",
                columns: new[] { "IngestionBatchId", "SubjectPersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionFindings_SubjectPersonId",
                table: "IngestionFindings",
                column: "SubjectPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipPreferences_ApplicationUserId_PersonId",
                table: "RelationshipPreferences",
                columns: new[] { "ApplicationUserId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipPreferences_PersonId",
                table: "RelationshipPreferences",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestionFindings");

            migrationBuilder.DropTable(
                name: "RelationshipPreferences");

            migrationBuilder.DropTable(
                name: "IngestionBatches");
        }
    }
}
