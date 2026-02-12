using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventAggregator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHtml = table.Column<string>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    MaxPeople = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Filters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    SearchKeyword = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ReportFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    LastReportSent = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserEventStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IsInCalendar = table.Column<bool>(type: "INTEGER", nullable: false),
                    MarkedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventStatuses_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventStatuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFilters",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFilters", x => new { x.UserId, x.FilterId });
                    table.ForeignKey(
                        name: "FK_UserFilters_Filters_FilterId",
                        column: x => x.FilterId,
                        principalTable: "Filters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFilters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEventStatuses_EventId",
                table: "UserEventStatuses",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventStatuses_UserId",
                table: "UserEventStatuses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFilters_FilterId",
                table: "UserFilters",
                column: "FilterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEventStatuses");

            migrationBuilder.DropTable(
                name: "UserFilters");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
