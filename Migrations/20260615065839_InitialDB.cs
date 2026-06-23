using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CMRL.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    attendanceid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    attendancedate = table.Column<DateOnly>(type: "date", nullable: false),
                    checkintime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checkouttime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    islate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance", x => x.attendanceid);
                });

            migrationBuilder.CreateTable(
                name: "auditlogs",
                columns: table => new
                {
                    logid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    actiontime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditlogs", x => x.logid);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    departmentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    departmentname = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.departmentid);
                });

            migrationBuilder.CreateTable(
                name: "leaves",
                columns: table => new
                {
                    leaveid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    fromdate = table.Column<DateOnly>(type: "date", nullable: false),
                    todate = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    approvedby = table.Column<int>(type: "integer", nullable: true),
                    appliedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaves", x => x.leaveid);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    isread = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.notificationid);
                });

            migrationBuilder.CreateTable(
                name: "otpstore",
                columns: table => new
                {
                    otpid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    otpcode = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiresat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isused = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otpstore", x => x.otpid);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    roleid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rolename = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.roleid);
                });

            migrationBuilder.CreateTable(
                name: "salary",
                columns: table => new
                {
                    salaryid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    basicsalary = table.Column<decimal>(type: "numeric", nullable: false),
                    leavecount = table.Column<int>(type: "integer", nullable: false),
                    deductionamount = table.Column<decimal>(type: "numeric", nullable: false),
                    finalsalary = table.Column<decimal>(type: "numeric", nullable: false),
                    isapproved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary", x => x.salaryid);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    settingid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    theme = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.settingid);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    shiftid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shiftname = table.Column<string>(type: "text", nullable: false),
                    starttime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    endtime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.shiftid);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    taskid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    taskname = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    assignedto = table.Column<int>(type: "integer", nullable: false),
                    assignedby = table.Column<int>(type: "integer", nullable: false),
                    startdate = table.Column<DateOnly>(type: "date", nullable: true),
                    duedate = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.taskid);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    roleid = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.userid);
                    table.ForeignKey(
                        name: "FK_users_roles_roleid",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "roleid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employeeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    fullname = table.Column<string>(type: "text", nullable: false),
                    departmentid = table.Column<int>(type: "integer", nullable: false),
                    designation = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    basicsalary = table.Column<decimal>(type: "numeric", nullable: false),
                    shiftid = table.Column<int>(type: "integer", nullable: false),
                    managerid = table.Column<int>(type: "integer", nullable: true),
                    hrid = table.Column<int>(type: "integer", nullable: true),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employeeid);
                    table.ForeignKey(
                        name: "FK_employees_departments_departmentid",
                        column: x => x.departmentid,
                        principalTable: "departments",
                        principalColumn: "departmentid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employees_shifts_shiftid",
                        column: x => x.shiftid,
                        principalTable: "shifts",
                        principalColumn: "shiftid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hr",
                columns: table => new
                {
                    hrid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    fullname = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    basicsalary = table.Column<decimal>(type: "numeric", nullable: false),
                    shiftid = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hr", x => x.hrid);
                    table.ForeignKey(
                        name: "FK_hr_shifts_shiftid",
                        column: x => x.shiftid,
                        principalTable: "shifts",
                        principalColumn: "shiftid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "managers",
                columns: table => new
                {
                    managerid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    fullname = table.Column<string>(type: "text", nullable: false),
                    departmentid = table.Column<int>(type: "integer", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    basicsalary = table.Column<decimal>(type: "numeric", nullable: false),
                    shiftid = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managers", x => x.managerid);
                    table.ForeignKey(
                        name: "FK_managers_departments_departmentid",
                        column: x => x.departmentid,
                        principalTable: "departments",
                        principalColumn: "departmentid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_managers_shifts_shiftid",
                        column: x => x.shiftid,
                        principalTable: "shifts",
                        principalColumn: "shiftid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_departmentid",
                table: "employees",
                column: "departmentid");

            migrationBuilder.CreateIndex(
                name: "IX_employees_shiftid",
                table: "employees",
                column: "shiftid");

            migrationBuilder.CreateIndex(
                name: "IX_hr_shiftid",
                table: "hr",
                column: "shiftid");

            migrationBuilder.CreateIndex(
                name: "IX_managers_departmentid",
                table: "managers",
                column: "departmentid");

            migrationBuilder.CreateIndex(
                name: "IX_managers_shiftid",
                table: "managers",
                column: "shiftid");

            migrationBuilder.CreateIndex(
                name: "IX_users_roleid",
                table: "users",
                column: "roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "auditlogs");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "hr");

            migrationBuilder.DropTable(
                name: "leaves");

            migrationBuilder.DropTable(
                name: "managers");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "otpstore");

            migrationBuilder.DropTable(
                name: "salary");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
