using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class dahfdgsfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonInteractions");

            migrationBuilder.DropTable(
                name: "PersonNotes");

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Notes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Interactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000006-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000002-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("1000000a-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000003-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000004-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000004-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000005-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000005-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000009-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000006-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000008-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000007-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000007-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000008-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000001-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000009-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000002-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a000000a-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000003-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000006-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000002-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000002-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000003-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000007-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000004-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000009-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000005-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000008-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000006-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000004-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000007-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("1000000a-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000008-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000001-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000009-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000005-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b000000a-0000-0000-0000-000000000000"),
                column: "PersonId",
                value: new Guid("10000003-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Notes_PersonId",
                table: "Notes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_PersonId",
                table: "Interactions",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interactions_Persons_PersonId",
                table: "Interactions",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Persons_PersonId",
                table: "Notes",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interactions_Persons_PersonId",
                table: "Interactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Persons_PersonId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_PersonId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Interactions_PersonId",
                table: "Interactions");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Interactions");

            migrationBuilder.CreateTable(
                name: "PersonInteractions",
                columns: table => new
                {
                    InteractionsInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonInteractions", x => new { x.InteractionsInteractionId, x.PeoplePersonId });
                    table.ForeignKey(
                        name: "FK_PersonInteractions_Interactions_InteractionsInteractionId",
                        column: x => x.InteractionsInteractionId,
                        principalTable: "Interactions",
                        principalColumn: "InteractionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonInteractions_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonNotes",
                columns: table => new
                {
                    NotesNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonNotes", x => new { x.NotesNoteId, x.PeoplePersonId });
                    table.ForeignKey(
                        name: "FK_PersonNotes_Notes_NotesNoteId",
                        column: x => x.NotesNoteId,
                        principalTable: "Notes",
                        principalColumn: "NoteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonNotes_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PersonInteractions",
                columns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") },
                    { new Guid("a0000002-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") },
                    { new Guid("a0000003-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") },
                    { new Guid("a0000003-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") },
                    { new Guid("a0000005-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") },
                    { new Guid("a0000006-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") },
                    { new Guid("a0000007-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") },
                    { new Guid("a0000008-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") },
                    { new Guid("a0000009-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") },
                    { new Guid("a000000a-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") }
                });

            migrationBuilder.InsertData(
                table: "PersonNotes",
                columns: new[] { "NotesNoteId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") },
                    { new Guid("b0000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") },
                    { new Guid("b0000003-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") },
                    { new Guid("b0000004-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") },
                    { new Guid("b0000005-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") },
                    { new Guid("b0000006-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") },
                    { new Guid("b0000007-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") },
                    { new Guid("b0000008-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") },
                    { new Guid("b0000009-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") },
                    { new Guid("b000000a-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonInteractions_PeoplePersonId",
                table: "PersonInteractions",
                column: "PeoplePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonNotes_PeoplePersonId",
                table: "PersonNotes",
                column: "PeoplePersonId");
        }
    }
}
