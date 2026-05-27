using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventAggregator.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeedSourceId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeedSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FeedUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastFetched = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_FeedSourceId",
                table: "Events",
                column: "FeedSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_FeedSources_FeedSourceId",
                table: "Events",
                column: "FeedSourceId",
                principalTable: "FeedSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_FeedSources_FeedSourceId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "FeedSources");

            migrationBuilder.DropIndex(
                name: "IX_Events_FeedSourceId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "FeedSourceId",
                table: "Events");
        }
    }
}
