using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingProfilePictureColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureContentType",
                table: "UserProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePictureData",
                table: "UserProfiles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureFileName",
                table: "UserProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfilePictureUploadedAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureContentType",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePictureData",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePictureFileName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUploadedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "UserProfiles");
        }
    }
}
