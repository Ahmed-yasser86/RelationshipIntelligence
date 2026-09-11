using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updatedatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("4a91b323-6902-4d3e-b147-3a2f6990c254"));

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("c1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("c2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d1111111-1111-1111-1111-111111111111"), new Guid("12345678-90ab-cdef-1234-567890abcdef") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d2222222-2222-2222-2222-222222222222"), new Guid("23456789-0abc-def1-2345-67890abcdefa") });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("12345678-90ab-cdef-1234-567890abcdef"), 8 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("23456789-0abc-def1-2345-67890abcdefa"), 2 });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("12345678-90ab-cdef-1234-567890abcdef"), new Guid("e2222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("23456789-0abc-def1-2345-67890abcdefa"), new Guid("e1111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("c1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("c2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("f1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("f2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("d1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("d2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("12345678-90ab-cdef-1234-567890abcdef"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("23456789-0abc-def1-2345-67890abcdefa"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("7c9e6645-3677-448a-95b7-511b41f17491"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6"));

            migrationBuilder.InsertData(
                table: "Circles",
                columns: new[] { "CircleId", "Name" },
                values: new object[,]
                {
                    { new Guid("f0000001-0000-0000-0000-000000000000"), "Inner Circle" },
                    { new Guid("f0000002-0000-0000-0000-000000000000"), "Computational Social Science Lab" },
                    { new Guid("f0000003-0000-0000-0000-000000000000"), "Capital University Alumni" },
                    { new Guid("f0000004-0000-0000-0000-000000000000"), "Proceedit Team" },
                    { new Guid("f0000005-0000-0000-0000-000000000000"), "Tokyo GCI Cohort" },
                    { new Guid("f0000006-0000-0000-0000-000000000000"), "Docker & k8s Devs" },
                    { new Guid("f0000007-0000-0000-0000-000000000000"), "Institutional Economics Book Club" },
                    { new Guid("f0000008-0000-0000-0000-000000000000"), "UC Berkeley Audit Group" },
                    { new Guid("f0000009-0000-0000-0000-000000000000"), "Historic Cairo Explorers" },
                    { new Guid("f000000a-0000-0000-0000-000000000000"), "C# Mentorship" }
                });

            migrationBuilder.InsertData(
                table: "ConnectionChannels",
                columns: new[] { "ConnectionChannelId", "ConnectionChannelName" },
                values: new object[,]
                {
                    { new Guid("d0000001-0000-0000-0000-000000000000"), "LinkedIn" },
                    { new Guid("d0000002-0000-0000-0000-000000000000"), "Twitter (X)" },
                    { new Guid("d0000003-0000-0000-0000-000000000000"), "GitHub" },
                    { new Guid("d0000004-0000-0000-0000-000000000000"), "Email" },
                    { new Guid("d0000005-0000-0000-0000-000000000000"), "WhatsApp" },
                    { new Guid("d0000006-0000-0000-0000-000000000000"), "Discord" },
                    { new Guid("d0000007-0000-0000-0000-000000000000"), "Slack" },
                    { new Guid("d0000008-0000-0000-0000-000000000000"), "Zoom" },
                    { new Guid("d0000009-0000-0000-0000-000000000000"), "Microsoft Teams" },
                    { new Guid("d000000a-0000-0000-0000-000000000000"), "Telegram" }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "CountryId", "CountryName" },
                values: new object[,]
                {
                    { new Guid("c0000001-0000-0000-0000-000000000000"), "Egypt" },
                    { new Guid("c0000002-0000-0000-0000-000000000000"), "Australia" },
                    { new Guid("c0000003-0000-0000-0000-000000000000"), "USA" },
                    { new Guid("c0000004-0000-0000-0000-000000000000"), "Japan" },
                    { new Guid("c0000005-0000-0000-0000-000000000000"), "Canada" },
                    { new Guid("c0000006-0000-0000-0000-000000000000"), "United Kingdom" },
                    { new Guid("c0000007-0000-0000-0000-000000000000"), "Germany" },
                    { new Guid("c0000008-0000-0000-0000-000000000000"), "France" },
                    { new Guid("c0000009-0000-0000-0000-000000000000"), "China" },
                    { new Guid("c000000a-0000-0000-0000-000000000000"), "Norway" }
                });

            migrationBuilder.InsertData(
                table: "Interactions",
                columns: new[] { "InteractionId", "InteractionDescription", "InteractionTitle", "InteractionType", "TimeOfInteraction" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000000"), null, "Research sync on structural waste", 0, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000002-0000-0000-0000-000000000000"), null, "Code Review: C# Web API", 0, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000003-0000-0000-0000-000000000000"), null, "Sociology debate on Durkheim", 0, new DateTime(2026, 6, 3, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000004-0000-0000-0000-000000000000"), null, "Kubernetes cluster troubleshooting", 0, new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000005-0000-0000-0000-000000000000"), null, "Cairo urban history walk planning", 0, new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000006-0000-0000-0000-000000000000"), null, "GCI World Data Science kickoff", 0, new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000007-0000-0000-0000-000000000000"), null, "GitHub Actions pairing session", 0, new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000008-0000-0000-0000-000000000000"), null, "Catchup call across time zones", 0, new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a0000009-0000-0000-0000-000000000000"), null, "Acemoglu reading discussion", 0, new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("a000000a-0000-0000-0000-000000000000"), null, "Institutional Economics framework mapping", 0, new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "NoteId", "Content", "NoteType" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000000"), "Discussed the framework of Douglass North regarding institutional constraints.", 1 },
                    { new Guid("b0000002-0000-0000-0000-000000000000"), "Finalized the scope for the paper: 'Structural Waste within Absorptive Structures: Egypt as a case'.", 2 },
                    { new Guid("b0000003-0000-0000-0000-000000000000"), "Troubleshooting the CI/CD pipeline. SonarCloud and Trivy are failing on the new C# build.", 1 },
                    { new Guid("b0000004-0000-0000-0000-000000000000"), "Need to schedule a visit to Beit Yakan and tour the Abdeen architectural sites.", 3 },
                    { new Guid("b0000005-0000-0000-0000-000000000000"), "Configured StatefulSets for the SQL Server database in Kubernetes.", 1 },
                    { new Guid("b0000006-0000-0000-0000-000000000000"), "Drafted the thank you email to Professor Ann Swidler for her sociology lectures.", 2 },
                    { new Guid("b0000007-0000-0000-0000-000000000000"), "Reviewing C# Repository Pattern and Serilog implementation for the Stock Management App.", 1 },
                    { new Guid("b0000008-0000-0000-0000-000000000000"), "Comparing cultural differences and social phenomena over a call.", 2 },
                    { new Guid("b0000009-0000-0000-0000-000000000000"), "Need to catch up on the latest UC Berkeley lecture notes.", 3 },
                    { new Guid("b000000a-0000-0000-0000-000000000000"), "Exploring the intersection of technical programming and CSS.", 1 }
                });

            migrationBuilder.InsertData(
                table: "SystemStatusTags",
                columns: new[] { "StatusTagId", "Description", "Name" },
                values: new object[,]
                {
                    { 3, "Spoke to them lately", "Recently Contacted" },
                    { 4, "Owe them a response", "Should Reply" },
                    { 5, "Awaiting their response", "Already Replied" },
                    { 6, "They haven't replied", "Ignored Me" },
                    { 7, "Skipped intentionally", "Ignored" },
                    { 9, "Get to it later", "Low Priority" }
                });

            migrationBuilder.InsertData(
                table: "UserDefinedTags",
                columns: new[] { "TagId", "TagName" },
                values: new object[,]
                {
                    { new Guid("e0000001-0000-0000-0000-000000000000"), "Software Engineering" },
                    { new Guid("e0000002-0000-0000-0000-000000000000"), "Economics Research" },
                    { new Guid("e0000003-0000-0000-0000-000000000000"), "Capital University" },
                    { new Guid("e0000004-0000-0000-0000-000000000000"), "Mentors" },
                    { new Guid("e0000005-0000-0000-0000-000000000000"), "Backend Devs" },
                    { new Guid("e0000006-0000-0000-0000-000000000000"), "Data Science" },
                    { new Guid("e0000007-0000-0000-0000-000000000000"), "Sociology" },
                    { new Guid("e0000008-0000-0000-0000-000000000000"), "Cairo History" },
                    { new Guid("e0000009-0000-0000-0000-000000000000"), "Networking" },
                    { new Guid("e000000a-0000-0000-0000-000000000000"), "Friends" }
                });

            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "PersonId", "Address", "CountryId", "DateOfBirth", "Gender", "Name", "NewsLetter", "email", "phone" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000000"), null, new Guid("c0000002-0000-0000-0000-000000000000"), new DateTime(1998, 4, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Ned Ibrahim", true, null, "+61411234567" },
                    { new Guid("10000002-0000-0000-0000-000000000000"), null, new Guid("c0000003-0000-0000-0000-000000000000"), new DateTime(1967, 9, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Daron Acemoglu", false, null, "+12025550174" },
                    { new Guid("10000003-0000-0000-0000-000000000000"), null, new Guid("c0000003-0000-0000-0000-000000000000"), new DateTime(1958, 9, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Michael Munger", false, null, "+19195550188" },
                    { new Guid("10000004-0000-0000-0000-000000000000"), null, new Guid("c0000003-0000-0000-0000-000000000000"), new DateTime(1944, 12, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Female", "Ann Swidler", true, null, "+15105550199" },
                    { new Guid("10000005-0000-0000-0000-000000000000"), null, new Guid("c0000008-0000-0000-0000-000000000000"), new DateTime(1858, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Emile Durkheim", false, null, "+3315550123" },
                    { new Guid("10000006-0000-0000-0000-000000000000"), null, new Guid("c0000003-0000-0000-0000-000000000000"), new DateTime(1920, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Douglass North", false, null, "+13145550145" },
                    { new Guid("10000007-0000-0000-0000-000000000000"), null, new Guid("c0000001-0000-0000-0000-000000000000"), new DateTime(2005, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Youssef", true, null, "+201012345678" },
                    { new Guid("10000008-0000-0000-0000-000000000000"), null, new Guid("c0000004-0000-0000-0000-000000000000"), new DateTime(2002, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Kenji", true, null, "+81312345678" },
                    { new Guid("10000009-0000-0000-0000-000000000000"), null, new Guid("c0000001-0000-0000-0000-000000000000"), new DateTime(2006, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Female", "Sarah", false, null, "+201112345678" },
                    { new Guid("1000000a-0000-0000-0000-000000000000"), null, new Guid("c0000001-0000-0000-0000-000000000000"), new DateTime(2001, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Omar", true, null, "+201212345678" }
                });

            migrationBuilder.InsertData(
                table: "ContactItemRoles",
                columns: new[] { "ContactsRoleId", "PersonId", "Role" },
                values: new object[,]
                {
                    { new Guid("20000001-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000003-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000004-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000005-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000006-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000007-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000008-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000"), "" },
                    { new Guid("20000009-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000"), "" },
                    { new Guid("2000000a-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000"), "" }
                });

            migrationBuilder.InsertData(
                table: "PersonCircles",
                columns: new[] { "CirclesCircleId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("f0000001-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") },
                    { new Guid("f0000003-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") },
                    { new Guid("f0000004-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") },
                    { new Guid("f0000005-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") },
                    { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") },
                    { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") },
                    { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") },
                    { new Guid("f0000008-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") },
                    { new Guid("f0000008-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") },
                    { new Guid("f000000a-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") }
                });

            migrationBuilder.InsertData(
                table: "PersonConnectionChannels",
                columns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                values: new object[,]
                {
                    { new Guid("d0000001-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") },
                    { new Guid("d0000001-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") },
                    { new Guid("d0000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") },
                    { new Guid("d0000003-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") },
                    { new Guid("d0000004-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") },
                    { new Guid("d0000004-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") },
                    { new Guid("d0000005-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") },
                    { new Guid("d0000006-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") },
                    { new Guid("d0000007-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") },
                    { new Guid("d0000008-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") }
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

            migrationBuilder.InsertData(
                table: "PersonSystemStatusTag",
                columns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000000"), 3 },
                    { new Guid("10000002-0000-0000-0000-000000000000"), 4 },
                    { new Guid("10000003-0000-0000-0000-000000000000"), 1 },
                    { new Guid("10000004-0000-0000-0000-000000000000"), 8 },
                    { new Guid("10000005-0000-0000-0000-000000000000"), 7 },
                    { new Guid("10000006-0000-0000-0000-000000000000"), 9 },
                    { new Guid("10000007-0000-0000-0000-000000000000"), 10 },
                    { new Guid("10000008-0000-0000-0000-000000000000"), 2 },
                    { new Guid("10000009-0000-0000-0000-000000000000"), 5 },
                    { new Guid("1000000a-0000-0000-0000-000000000000"), 6 }
                });

            migrationBuilder.InsertData(
                table: "PersonUserDefinedTags",
                columns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000000"), new Guid("e000000a-0000-0000-0000-000000000000") },
                    { new Guid("10000002-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") },
                    { new Guid("10000003-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") },
                    { new Guid("10000004-0000-0000-0000-000000000000"), new Guid("e0000007-0000-0000-0000-000000000000") },
                    { new Guid("10000005-0000-0000-0000-000000000000"), new Guid("e0000007-0000-0000-0000-000000000000") },
                    { new Guid("10000006-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") },
                    { new Guid("10000007-0000-0000-0000-000000000000"), new Guid("e0000001-0000-0000-0000-000000000000") },
                    { new Guid("10000008-0000-0000-0000-000000000000"), new Guid("e0000006-0000-0000-0000-000000000000") },
                    { new Guid("10000009-0000-0000-0000-000000000000"), new Guid("e0000003-0000-0000-0000-000000000000") },
                    { new Guid("1000000a-0000-0000-0000-000000000000"), new Guid("e0000005-0000-0000-0000-000000000000") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("20000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ContactItemRoles",
                keyColumn: "ContactsRoleId",
                keyValue: new Guid("2000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000001-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000003-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000004-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000005-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000007-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000008-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f0000008-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonCircles",
                keyColumns: new[] { "CirclesCircleId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("f000000a-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000001-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000001-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000003-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000004-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000004-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000005-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000006-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000007-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonConnectionChannels",
                keyColumns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("d0000008-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000002-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000003-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000003-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000005-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000006-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000007-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000008-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a0000009-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonInteractions",
                keyColumns: new[] { "InteractionsInteractionId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("a000000a-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000001-0000-0000-0000-000000000000"), new Guid("10000006-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000002-0000-0000-0000-000000000000"), new Guid("10000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000003-0000-0000-0000-000000000000"), new Guid("10000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000004-0000-0000-0000-000000000000"), new Guid("10000009-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000005-0000-0000-0000-000000000000"), new Guid("10000008-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000006-0000-0000-0000-000000000000"), new Guid("10000004-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000007-0000-0000-0000-000000000000"), new Guid("1000000a-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000008-0000-0000-0000-000000000000"), new Guid("10000001-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b0000009-0000-0000-0000-000000000000"), new Guid("10000005-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonNotes",
                keyColumns: new[] { "NotesNoteId", "PeoplePersonId" },
                keyValues: new object[] { new Guid("b000000a-0000-0000-0000-000000000000"), new Guid("10000003-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000001-0000-0000-0000-000000000000"), 3 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000002-0000-0000-0000-000000000000"), 4 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000003-0000-0000-0000-000000000000"), 1 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000004-0000-0000-0000-000000000000"), 8 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000005-0000-0000-0000-000000000000"), 7 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000006-0000-0000-0000-000000000000"), 9 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000007-0000-0000-0000-000000000000"), 10 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000008-0000-0000-0000-000000000000"), 2 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000009-0000-0000-0000-000000000000"), 5 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("1000000a-0000-0000-0000-000000000000"), 6 });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000001-0000-0000-0000-000000000000"), new Guid("e000000a-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000002-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000003-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000004-0000-0000-0000-000000000000"), new Guid("e0000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000005-0000-0000-0000-000000000000"), new Guid("e0000007-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000006-0000-0000-0000-000000000000"), new Guid("e0000002-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000007-0000-0000-0000-000000000000"), new Guid("e0000001-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000008-0000-0000-0000-000000000000"), new Guid("e0000006-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("10000009-0000-0000-0000-000000000000"), new Guid("e0000003-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "PersonUserDefinedTags",
                keyColumns: new[] { "PeoplePersonId", "UserDefinedTagsTagId" },
                keyValues: new object[] { new Guid("1000000a-0000-0000-0000-000000000000"), new Guid("e0000005-0000-0000-0000-000000000000") });

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f0000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Circles",
                keyColumn: "CircleId",
                keyValue: new Guid("f000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "ConnectionChannels",
                keyColumn: "ConnectionChannelId",
                keyValue: new Guid("d0000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Interactions",
                keyColumn: "InteractionId",
                keyValue: new Guid("a000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b0000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "NoteId",
                keyValue: new Guid("b000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000008-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("10000009-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonId",
                keyValue: new Guid("1000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000005-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000006-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e0000007-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "UserDefinedTags",
                keyColumn: "TagId",
                keyValue: new Guid("e000000a-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "CountryId",
                keyValue: new Guid("c0000008-0000-0000-0000-000000000000"));

            migrationBuilder.InsertData(
                table: "Circles",
                columns: new[] { "CircleId", "Name" },
                values: new object[,]
                {
                    { new Guid("c1111111-1111-1111-1111-111111111111"), "Inner Circle" },
                    { new Guid("c2222222-2222-2222-2222-222222222222"), "Professional Network" }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "CountryId", "CountryName" },
                values: new object[,]
                {
                    { new Guid("4a91b323-6902-4d3e-b147-3a2f6990c254"), "Norway" },
                    { new Guid("7c9e6645-3677-448a-95b7-511b41f17491"), "Japan" },
                    { new Guid("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6"), "Canada" }
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

            migrationBuilder.InsertData(
                table: "UserDefinedTags",
                columns: new[] { "TagId", "TagName" },
                values: new object[,]
                {
                    { new Guid("e1111111-1111-1111-1111-111111111111"), "Work" },
                    { new Guid("e2222222-2222-2222-2222-222222222222"), "Friends" }
                });

            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "PersonId", "Address", "CountryId", "DateOfBirth", "Gender", "Name", "NewsLetter", "email", "phone" },
                values: new object[,]
                {
                    { new Guid("12345678-90ab-cdef-1234-567890abcdef"), null, new Guid("7c9e6645-3677-448a-95b7-511b41f17491"), new DateTime(1996, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Female", "Alice", false, null, "1111111111" },
                    { new Guid("23456789-0abc-def1-2345-67890abcdefa"), null, new Guid("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6"), new DateTime(2001, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Bob", true, null, "2222222222" }
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
        }
    }
}
