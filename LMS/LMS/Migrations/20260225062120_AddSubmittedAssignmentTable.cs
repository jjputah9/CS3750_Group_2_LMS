using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "submittedAssignments",
                columns: table => new
                {
                    submittedAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    submissionTypeId = table.Column<int>(type: "int", nullable: false),
                    filePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    submissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    textSubmission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    grade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submittedAssignments", x => x.submittedAssignmentId);
                    table.ForeignKey(
                        name: "FK_submittedAssignments_SubmissionType_submissionTypeId",
                        column: x => x.submissionTypeId,
                        principalTable: "SubmissionType",
                        principalColumn: "SubmissionTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_submittedAssignments_submissionTypeId",
                table: "submittedAssignments",
                column: "submissionTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submittedAssignments");
        }
    }
}
