using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreferenceAuditAndGlobalDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalPreferenceDefaults",
                columns: table => new
                {
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultCadenceDays = table.Column<int>(type: "int", nullable: true),
                    DefaultReminderStrict = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalPreferenceDefaults", x => x.ApplicationUserId);
                });

            migrationBuilder.CreateTable(
                name: "PreferenceAuditEntries",
                columns: table => new
                {
                    PreferenceAuditEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenceAuditEntries", x => x.PreferenceAuditEntryId);
                    table.ForeignKey(
                        name: "FK_PreferenceAuditEntries_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceAuditEntries_ApplicationUserId_PersonId_ChangedAtUtc",
                table: "PreferenceAuditEntries",
                columns: new[] { "ApplicationUserId", "PersonId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceAuditEntries_PersonId",
                table: "PreferenceAuditEntries",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalPreferenceDefaults");

            migrationBuilder.DropTable(
                name: "PreferenceAuditEntries");
        }
    }
}
