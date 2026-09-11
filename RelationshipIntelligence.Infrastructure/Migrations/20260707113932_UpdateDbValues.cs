using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000001-0000-0000-0000-000000000000"), 3 });

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
                keyValues: new object[] { new Guid("10000007-0000-0000-0000-000000000000"), 10 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000009-0000-0000-0000-000000000000"), 5 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("1000000a-0000-0000-0000-000000000000"), 6 });

            migrationBuilder.InsertData(
                table: "PersonSystemStatusTag",
                columns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000000"), 7 },
                    { new Guid("10000003-0000-0000-0000-000000000000"), 5 },
                    { new Guid("10000004-0000-0000-0000-000000000000"), 1 },
                    { new Guid("10000005-0000-0000-0000-000000000000"), 10 },
                    { new Guid("10000007-0000-0000-0000-000000000000"), 6 },
                    { new Guid("10000009-0000-0000-0000-000000000000"), 8 },
                    { new Guid("1000000a-0000-0000-0000-000000000000"), 3 }
                });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Immediate attention needed", "High Priority" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "They haven't replied", "Ignored Me" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Needs follow up", "Follow Up" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Standard priority attention", "Moderate Priority" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 7,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Spoke to them lately", "Recently Contacted" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 8,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Awaiting their response", "Already Replied" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 10,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Skipped intentionally", "Ignored" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000001-0000-0000-0000-000000000000"), 7 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000003-0000-0000-0000-000000000000"), 5 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000004-0000-0000-0000-000000000000"), 1 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000005-0000-0000-0000-000000000000"), 10 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000007-0000-0000-0000-000000000000"), 6 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("10000009-0000-0000-0000-000000000000"), 8 });

            migrationBuilder.DeleteData(
                table: "PersonSystemStatusTag",
                keyColumns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                keyValues: new object[] { new Guid("1000000a-0000-0000-0000-000000000000"), 3 });

            migrationBuilder.InsertData(
                table: "PersonSystemStatusTag",
                columns: new[] { "PeoplePersonId", "SystemStatusTagsStatusTagId" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000000"), 3 },
                    { new Guid("10000003-0000-0000-0000-000000000000"), 1 },
                    { new Guid("10000004-0000-0000-0000-000000000000"), 8 },
                    { new Guid("10000005-0000-0000-0000-000000000000"), 7 },
                    { new Guid("10000007-0000-0000-0000-000000000000"), 10 },
                    { new Guid("10000009-0000-0000-0000-000000000000"), 5 },
                    { new Guid("1000000a-0000-0000-0000-000000000000"), 6 }
                });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Needs follow up", "Follow Up" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Spoke to them lately", "Recently Contacted" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Awaiting their response", "Already Replied" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "They haven't replied", "Ignored Me" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 7,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Skipped intentionally", "Ignored" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 8,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Immediate attention needed", "High Priority" });

            migrationBuilder.UpdateData(
                table: "SystemStatusTags",
                keyColumn: "StatusTagId",
                keyValue: 10,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Standard priority attention", "Moderate Priority" });
        }
    }
}
