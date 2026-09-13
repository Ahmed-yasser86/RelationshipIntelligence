using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class Outreach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutreachBatches",
                columns: table => new
                {
                    OutreachBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    GlobalInstruction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutreachBatches", x => x.OutreachBatchId);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationDrafts",
                columns: table => new
                {
                    CommunicationDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutreachBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContextUsed = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LimitedContext = table.Column<bool>(type: "bit", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationDrafts", x => x.CommunicationDraftId);
                    table.ForeignKey(
                        name: "FK_CommunicationDrafts_OutreachBatches_OutreachBatchId",
                        column: x => x.OutreachBatchId,
                        principalTable: "OutreachBatches",
                        principalColumn: "OutreachBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationDrafts_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutreachBatchMembers",
                columns: table => new
                {
                    OutreachBatchMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutreachBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChannelOverride = table.Column<int>(type: "int", nullable: true),
                    IntentOverride = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomInstruction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Excluded = table.Column<bool>(type: "bit", nullable: false),
                    SkipFutureSuggestions = table.Column<bool>(type: "bit", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutreachBatchMembers", x => x.OutreachBatchMemberId);
                    table.ForeignKey(
                        name: "FK_OutreachBatchMembers_OutreachBatches_OutreachBatchId",
                        column: x => x.OutreachBatchId,
                        principalTable: "OutreachBatches",
                        principalColumn: "OutreachBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutreachBatchMembers_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationDrafts_OutreachBatchId_PersonId",
                table: "CommunicationDrafts",
                columns: new[] { "OutreachBatchId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationDrafts_PersonId",
                table: "CommunicationDrafts",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OutreachBatches_ApplicationUserId_Status",
                table: "OutreachBatches",
                columns: new[] { "ApplicationUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OutreachBatchMembers_OutreachBatchId",
                table: "OutreachBatchMembers",
                column: "OutreachBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OutreachBatchMembers_PersonId",
                table: "OutreachBatchMembers",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationDrafts");

            migrationBuilder.DropTable(
                name: "OutreachBatchMembers");

            migrationBuilder.DropTable(
                name: "OutreachBatches");
        }
    }
}
