using DabaTaseApp.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DabaTaseApp.Migrations
{
    [DbContext(typeof(Lab1Context))]
    [Migration("20260503083000_AddStudentIdentityLink")]
    public partial class AddStudentIdentityLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "application_user_id",
                table: "students",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_application_user_id",
                table: "students",
                column: "application_user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "students_application_user_id_fkey",
                table: "students",
                column: "application_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "students_application_user_id_fkey",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_application_user_id",
                table: "students");

            migrationBuilder.DropColumn(
                name: "application_user_id",
                table: "students");
        }
    }
}
