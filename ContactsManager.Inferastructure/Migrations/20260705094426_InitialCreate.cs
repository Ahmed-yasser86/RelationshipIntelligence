using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextMemory",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInProfile",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherInformation",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SocialMediaAccounts",
                columns: table => new
                {
                    SocialMediaAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialMediaAccounts", x => x.SocialMediaAccountId);
                });

            migrationBuilder.CreateTable(
                name: "PersonSocialMediaAccounts",
                columns: table => new
                {
                    OtherSocialMediaAccountsSocialMediaAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeoplePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonSocialMediaAccounts", x => new { x.OtherSocialMediaAccountsSocialMediaAccountId, x.PeoplePersonId });
                    table.ForeignKey(
                        name: "FK_PersonSocialMediaAccounts_Persons_PeoplePersonId",
                        column: x => x.PeoplePersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonSocialMediaAccounts_SocialMediaAccounts_OtherSocialMediaAccountsSocialMediaAccountId",
                        column: x => x.OtherSocialMediaAccountsSocialMediaAccountId,
                        principalTable: "SocialMediaAccounts",
                        principalColumn: "SocialMediaAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000002-0000-0000-0000-000000000000"),
                column: "Content",
                value: "Finalized the scope for the paper: 'Structural Waste within Absorptive Structures'.");

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000003-0000-0000-0000-000000000000"),
                column: "NoteType",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Sydney, NSW", "Great conversations on social phenomena", "linkedin.com/in/ned-ibrahim", "Met online to discuss cultural differences", null, null, "ned@example.com" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000002-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Cambridge, MA", "Author of Why Nations Fail", "linkedin.com/in/dacemoglu", "Institutional Economics research", null, null, "daron@mit.edu" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000003-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Durham, NC", "Excellent pedagogical style in lectures", "linkedin.com/in/mmunger", "Academic lectures contact", null, null, "munger@duke.edu" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000004-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Berkeley, CA", "Culture in Action sociology frameworks", "linkedin.com/in/aswidler", "UC Berkeley Audit", null, null, "swidler@berkeley.edu" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000005-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Paris, France", "Structural functionalism architect", "", "Sociology foundational reading", null, null, "emile@sorbonne.fr" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000006-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "St. Louis, MO", "Nobel laureate in economics", "", "Institutional constraints research", null, null, "north@wustl.edu" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000007-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Cairo, Egypt", "Classmate in BIS academic program", "linkedin.com/in/youssef-bis", "Capital University BIS", null, null, "youssef@capital.edu.eg" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000008-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Tokyo, Japan", "Data Science program partner", "linkedin.com/in/kenji-data", "Tokyo GCI Cohort", null, null, "kenji@tokyo.ac.jp" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000009-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Cairo, Egypt", "Software Engineering intern colleague", "linkedin.com/in/sarah-dev", "Proceedit Internship", null, null, "sarah@proceedit.com" });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("1000000a-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "ContextMemory", "LinkedInProfile", "Origin", "OtherInformation", "ProfileImagePath", "email" },
                values: new object[] { "Giza, Egypt", "Met at Beit Yakan architectural tour", "linkedin.com/in/omar-arch", "Historic Cairo Explorers", null, null, "omar@cairohistory.org" });

            migrationBuilder.InsertData(
                table: "SocialMediaAccounts",
                columns: new[] { "SocialMediaAccountId", "Platform", "Url" },
                values: new object[,]
                {
                    { new Guid("90000001-0000-0000-0000-000000000000"), "LinkedIn", "https://linkedin.com/in/ned-ibrahim" },
                    { new Guid("90000002-0000-0000-0000-000000000000"), "Twitter", "https://twitter.com/acemoglu" },
                    { new Guid("90000003-0000-0000-0000-000000000000"), "GitHub", "https://github.com/munger-economics" },
                    { new Guid("90000004-0000-0000-0000-000000000000"), "ResearchGate", "https://researchgate.net/profile/ann-swidler" },
                    { new Guid("90000005-0000-0000-0000-000000000000"), "Wikipedia", "https://en.wikipedia.org/wiki/Emile_Durkheim" },
                    { new Guid("90000006-0000-0000-0000-000000000000"), "NobelPrize", "https://nobelprize.org/douglass-north" },
                    { new Guid("90000007-0000-0000-0000-000000000000"), "LinkedIn", "https://linkedin.com/in/youssef-dev" },
                    { new Guid("90000008-0000-0000-0000-000000000000"), "GitHub", "https://github.com/kenji-gci" },
                    { new Guid("90000009-0000-0000-0000-000000000000"), "Instagram", "https://instagram.com/sarah_cairo" },
                    { new Guid("9000000a-0000-0000-0000-000000000000"), "Twitter", "https://twitter.com/omar_tech" }
                });

            migrationBuilder.InsertData(
                table: "PersonSocialMediaAccounts",
                columns: new[] { "OtherSocialMediaAccountsSocialMediaAccountId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("90000001-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") },
                    { new Guid("90000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") },
                    { new Guid("90000003-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") },
                    { new Guid("90000004-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") },
                    { new Guid("90000005-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") },
                    { new Guid("90000006-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") },
                    { new Guid("90000007-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") },
                    { new Guid("90000008-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") },
                    { new Guid("90000009-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") },
                    { new Guid("9000000a-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonSocialMediaAccounts_PeoplePersonId",
                table: "PersonSocialMediaAccounts",
                column: "PeoplePersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonSocialMediaAccounts");

            migrationBuilder.DropTable(
                name: "SocialMediaAccounts");

            migrationBuilder.DropColumn(
                name: "ContextMemory",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LinkedInProfile",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "OtherInformation",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "Persons");

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000002-0000-0000-0000-000000000000"),
                column: "Content",
                value: "Finalized the scope for the paper: 'Structural Waste within Absorptive Structures: Egypt as a case'.");

            migrationBuilder.UpdateData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000003-0000-0000-0000-000000000000"),
                column: "NoteType",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000002-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000003-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000004-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000005-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000006-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000007-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000008-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000009-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("1000000a-0000-0000-0000-000000000000"),
                columns: new[] { "Address", "email" },
                values: new object[] { null, null });
        }
    }
}
