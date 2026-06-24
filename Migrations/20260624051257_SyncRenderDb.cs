using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CMRL.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncRenderDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "absentdays",
                table: "salary",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "absentdeduction",
                table: "salary",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "latecount",
                table: "salary",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latededuction",
                table: "salary",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "leavededuction",
                table: "salary",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "holidays",
                columns: table => new
                {
                    holidayid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    holidaydate = table.Column<DateOnly>(type: "date", nullable: false),
                    holidayname = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holidays", x => x.holidayid);
                });

            migrationBuilder.CreateTable(
                name: "shiftswaprequests",
                columns: table => new
                {
                    requestid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    currentshiftid = table.Column<int>(type: "integer", nullable: false),
                    requestedshiftid = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    requestedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approvedby = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shiftswaprequests", x => x.requestid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holidays");

            migrationBuilder.DropTable(
                name: "shiftswaprequests");

            migrationBuilder.DropColumn(
                name: "absentdays",
                table: "salary");

            migrationBuilder.DropColumn(
                name: "absentdeduction",
                table: "salary");

            migrationBuilder.DropColumn(
                name: "latecount",
                table: "salary");

            migrationBuilder.DropColumn(
                name: "latededuction",
                table: "salary");

            migrationBuilder.DropColumn(
                name: "leavededuction",
                table: "salary");
        }
    }
}
