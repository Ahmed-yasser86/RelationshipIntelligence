using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContactsManager.Inferastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactChannelValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonConnectionChannels",
                table: "PersonConnectionChannels");

            migrationBuilder.DropIndex(
                name: "IX_PersonConnectionChannels_PeoplePersonId",
                table: "PersonConnectionChannels");

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

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "PersonConnectionChannels",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonConnectionChannels",
                table: "PersonConnectionChannels",
                columns: new[] { "PeoplePersonId", "ConnectionChannelsConnectionChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonConnectionChannels_ConnectionChannelsConnectionChannelId",
                table: "PersonConnectionChannels",
                column: "ConnectionChannelsConnectionChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonConnectionChannels",
                table: "PersonConnectionChannels");

            migrationBuilder.DropIndex(
                name: "IX_PersonConnectionChannels_ConnectionChannelsConnectionChannelId",
                table: "PersonConnectionChannels");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "PersonConnectionChannels");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonConnectionChannels",
                table: "PersonConnectionChannels",
                columns: new[] { "ConnectionChannelsConnectionChannelId", "PeoplePersonId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PersonConnectionChannels_PeoplePersonId",
                table: "PersonConnectionChannels",
                column: "PeoplePersonId");
        }
    }
}
