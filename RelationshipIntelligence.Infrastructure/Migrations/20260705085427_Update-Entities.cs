using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("34567890-abcd-ef12-3456-7890abcdefa1"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("4567890a-bcde-f123-4567-890abcdefa12"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("567890ab-cdef-1234-5678-90abcdefa123"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("99c6a23d-8d1e-4e90-95b6-03b576c75f71"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("f2345b12-1111-4a55-89cc-5521aabbccdd"));

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    CircleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.CircleId);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionChannels",
                columns: table => new
                {
                    ConnectionChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionChannelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionChannels", x => x.ConnectionChannelId);
                });

            migrationBuilder.CreateTable(
                name: "ContactItemRoles",
                columns: table => new
                {
                    ContactsRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactItemRoles", x => x.ContactsRoleId);
                    table.ForeignKey(
                        name: "FK_ContactItemRoles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interactions",
                columns: table => new
                {
                    InteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InteractionType = table.Column<int>(type: "int", nullable: false),
                    InteractionTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InteractionDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeOfInteraction = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interactions", x => x.InteractionId);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.NoteId);
                });

            migrationBuilder.CreateTable(
                name: "SystemStatusTags",
                columns: table => new
                {
                    StatusTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemStatusTags", x => x.StatusTagId);
                });

            migrationBuilder.CreateTable(
                name: "UserDefinedTags",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDefinedTags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "PersonCircles",
                columns: table => new
                {
                    CirclesCircleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonCircles", x => new { x.CirclesCircleId, x.PeoplePersonId });
                    table.ForeignKey(
                        name: "FK_PersonCircles_Circles_CirclesCircleId",
                        column: x => x.CirclesCircleId,
                        principalTable: "Circles",
                        principalColumn: "CircleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonCircles_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonConnectionChannels",
                columns: table => new
                {
                    ConnectionChannelsConnectionChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonConnectionChannels", x => new { x.ConnectionChannelsConnectionChannelId, x.PeoplePersonId });
                    table.ForeignKey(
                        name: "FK_PersonConnectionChannels_ConnectionChannels_ConnectionChannelsConnectionChannelId",
                        column: x => x.ConnectionChannelsConnectionChannelId,
                        principalTable: "ConnectionChannels",
                        principalColumn: "ConnectionChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonConnectionChannels_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "PersonSystemStatusTag",
                columns: table => new
                {
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemStatusTagsStatusTagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonSystemStatusTag", x => new { x.PeoplePersonId, x.SystemStatusTagsStatusTagId });
                    table.ForeignKey(
                        name: "FK_PersonSystemStatusTag_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonSystemStatusTag_SystemStatusTags_SystemStatusTagsStatusTagId",
                        column: x => x.SystemStatusTagsStatusTagId,
                        principalTable: "SystemStatusTags",
                        principalColumn: "StatusTagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonUserDefinedTags",
                columns: table => new
                {
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserDefinedTagsTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonUserDefinedTags", x => new { x.PeoplePersonId, x.UserDefinedTagsTagId });
                    table.ForeignKey(
                        name: "FK_PersonUserDefinedTags_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonUserDefinedTags_UserDefinedTags_UserDefinedTagsTagId",
                        column: x => x.UserDefinedTagsTagId,
                        principalTable: "UserDefinedTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Circles",
                columns: new[] { "CircleId", "Name" },
                values: new object[,]
                {
                    { new Guid("c1111111-1111-1111-1111-111111111111"), "Inner Circle" },
                    { new Guid("c2222222-2222-2222-2222-222222222222"), "Professional Network" }
                });

            migrationBuilder.InsertData(
                table: "ContactItemRoles",
                columns: new[] { "ContactsRoleId", "PersonId", "Role" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef"), "" },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa"), "" }
                });

            migrationBuilder.InsertData(
                table: "Interactions",
                columns: new[] { "InteractionId", "InteractionDescription", "InteractionTitle", "InteractionType", "TimeOfInteraction" },
                values: new object[,]
                {
                    { new Guid("f1111111-1111-1111-1111-111111111111"), null, "Introductory Phone Call", 0, new DateTime(2026, 6, 15, 14, 30, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("f2222222-2222-2222-2222-222222222222"), null, "Strategy sync via Email", 0, new DateTime(2026, 7, 1, 9, 15, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "NoteId", "Content", "NoteType" },
                values: new object[,]
                {
                    { new Guid("d1111111-1111-1111-1111-111111111111"), "Met at the conference. Prefers email updates.", 1 },
                    { new Guid("d2222222-2222-2222-2222-222222222222"), "Discussed potential collaboration on the C# engine.", 3 }
                });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("12345678-90ab-cdef-1234-567890abcdef"),
                column: "NewsLetter",
                value: false);

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("23456789-0abc-def1-2345-67890abcdefa"),
                column: "NewsLetter",
                value: true);

            migrationBuilder.InsertData(
                table: "SystemStatusTags",
                columns: new[] { "StatusTagId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Needs follow up", "Follow Up" },
                    { 2, "Urgent follow up required", "Urgent" },
                    { 8, "Immediate attention needed", "High Priority" },
                    { 10, "Standard priority attention", "Moderate Priority" }
                });

            migrationBuilder.InsertData(
                table: "UserDefinedTags",
                columns: new[] { "TagId", "TagName" },
                values: new object[,]
                {
                    { new Guid("e1111111-1111-1111-1111-111111111111"), "Work" },
                    { new Guid("e2222222-2222-2222-2222-222222222222"), "Friends" }
                });

            migrationBuilder.InsertData(
                table: "PersonCircles",
                columns: new[] { "CirclesCircleId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("c1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") },
                    { new Guid("c2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") }
                });

            migrationBuilder.InsertData(
                table: "PersonInteractions",
                columns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("f1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") },
                    { new Guid("f2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") }
                });

            migrationBuilder.InsertData(
                table: "PersonNotes",
                columns: new[] { "NotesNoteId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("d1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") },
                    { new Guid("d2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") }
                });

            migrationBuilder.InsertData(
                table: "PersonSystemStatusTag",
                columns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                values: new object[,]
                {
                    { new Guid("12345678-90ab-cdef-1234-567890abcdef"), 8 },
                    { new Guid("23456789-0abc-def1-2345-67890abcdefa"), 2 }
                });

            migrationBuilder.InsertData(
                table: "PersonUserDefinedTags",
                columns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                values: new object[,]
                {
                    { new Guid("12345678-90ab-cdef-1234-567890abcdef"), new Guid("e2222222-2222-2222-2222-222222222222") },
                    { new Guid("23456789-0abc-def1-2345-67890abcdefa"), new Guid("e1111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactItemRoles_PersonId",
                table: "ContactItemRoles",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonCircles_PeoplePersonId",
                table: "PersonCircles",
                column: "PeoplePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonConnectionChannels_PeoplePersonId",
                table: "PersonConnectionChannels",
                column: "PeoplePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonInteractions_PeoplePersonId",
                table: "PersonInteractions",
                column: "PeoplePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonNotes_PeoplePersonId",
                table: "PersonNotes",
                column: "PeoplePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonSystemStatusTag_SystemStatusTagsStatusTagId",
                table: "PersonSystemStatusTag",
                column: "SystemStatusTagsStatusTagId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonUserDefinedTags_UserDefinedTagsTagId",
                table: "PersonUserDefinedTags",
                column: "UserDefinedTagsTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactItemRoles");

            migrationBuilder.DropTable(
                name: "PersonCircles");

            migrationBuilder.DropTable(
                name: "PersonConnectionChannels");

            migrationBuilder.DropTable(
                name: "PersonInteractions");

            migrationBuilder.DropTable(
                name: "PersonNotes");

            migrationBuilder.DropTable(
                name: "PersonSystemStatusTag");

            migrationBuilder.DropTable(
                name: "PersonUserDefinedTags");

            migrationBuilder.DropTable(
                name: "Circles");

            migrationBuilder.DropTable(
                name: "ConnectionChannels");

            migrationBuilder.DropTable(
                name: "Interactions");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "SystemStatusTags");

            migrationBuilder.DropTable(
                name: "UserDefinedTags");

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "CountryId", "CountryName" },
                values: new object[,]
                {
                    { new Guid("99c6a23d-8d1e-4e90-95b6-03b576c75f71"), "Australia" },
                    { new Guid("f2345b12-1111-4a55-89cc-5521aabbccdd"), "Brazil" }
                });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("12345678-90ab-cdef-1234-567890abcdef"),
                column: "NewsLetter",
                value: null);

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("23456789-0abc-def1-2345-67890abcdefa"),
                column: "NewsLetter",
                value: null);

            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "PersonId", "Address", "CountryId", "DateOfBirth", "Gender", "Name", "NewsLetter", "email", "phone" },
                values: new object[,]
                {
                    { new Guid("34567890-abcd-ef12-3456-7890abcdefa1"), null, new Guid("4a91b323-6902-4d3e-b147-3a2f6990c254"), new DateTime(1991, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Charlie", null, null, "3333333333" },
                    { new Guid("4567890a-bcde-f123-4567-890abcdefa12"), null, new Guid("99c6a23d-8d1e-4e90-95b6-03b576c75f71"), new DateTime(1998, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "David", null, null, "4444444444" },
                    { new Guid("567890ab-cdef-1234-5678-90abcdefa123"), null, new Guid("f2345b12-1111-4a55-89cc-5521aabbccdd"), new DateTime(2004, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Female", "Eve", null, null, "5555555555" }
                });
        }
    }
}
