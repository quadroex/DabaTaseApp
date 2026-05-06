using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DabaTaseApp.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeAccountsMoneyAndAccessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "groups_theory_instructor_id_fkey",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "instructor_categories_category_id_fkey",
                table: "instructor_categories");

            migrationBuilder.DropForeignKey(
                name: "instructor_categories_instructors_id_fkey",
                table: "instructor_categories");

            migrationBuilder.DropForeignKey(
                name: "payments_student_id_fkey",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_instructor_id_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_student_id_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_vehicle_plate_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "students_group_id_fkey",
                table: "students");

            migrationBuilder.DropForeignKey(
                name: "theory_sessions_group_id_fkey",
                table: "theory_sessions");

            migrationBuilder.DropForeignKey(
                name: "theory_sessions_instructor_id_fkey",
                table: "theory_sessions");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "students",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "balance",
                table: "students",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "application_user_id",
                table: "instructors",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_instructors_application_user_id",
                table: "instructors",
                column: "application_user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "groups_theory_instructor_id_fkey",
                table: "groups",
                column: "theory_instructor_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "instructor_categories_category_id_fkey",
                table: "instructor_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "instructor_categories_instructors_id_fkey",
                table: "instructor_categories",
                column: "instructors_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "instructors_application_user_id_fkey",
                table: "instructors",
                column: "application_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "payments_student_id_fkey",
                table: "payments",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_instructor_id_fkey",
                table: "practice_sessions",
                column: "instructor_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_student_id_fkey",
                table: "practice_sessions",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_vehicle_plate_fkey",
                table: "practice_sessions",
                column: "vehicle_plate",
                principalTable: "vehicles",
                principalColumn: "plate_number",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "students_group_id_fkey",
                table: "students",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "theory_sessions_group_id_fkey",
                table: "theory_sessions",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "theory_sessions_instructor_id_fkey",
                table: "theory_sessions",
                column: "instructor_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "groups_theory_instructor_id_fkey",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "instructor_categories_category_id_fkey",
                table: "instructor_categories");

            migrationBuilder.DropForeignKey(
                name: "instructor_categories_instructors_id_fkey",
                table: "instructor_categories");

            migrationBuilder.DropForeignKey(
                name: "instructors_application_user_id_fkey",
                table: "instructors");

            migrationBuilder.DropForeignKey(
                name: "payments_student_id_fkey",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_instructor_id_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_student_id_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "practice_sessions_vehicle_plate_fkey",
                table: "practice_sessions");

            migrationBuilder.DropForeignKey(
                name: "students_group_id_fkey",
                table: "students");

            migrationBuilder.DropForeignKey(
                name: "theory_sessions_group_id_fkey",
                table: "theory_sessions");

            migrationBuilder.DropForeignKey(
                name: "theory_sessions_instructor_id_fkey",
                table: "theory_sessions");

            migrationBuilder.DropIndex(
                name: "IX_instructors_application_user_id",
                table: "instructors");

            migrationBuilder.DropColumn(
                name: "application_user_id",
                table: "instructors");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "students",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<int>(
                name: "balance",
                table: "students",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<int>(
                name: "amount",
                table: "payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AddForeignKey(
                name: "groups_theory_instructor_id_fkey",
                table: "groups",
                column: "theory_instructor_id",
                principalTable: "instructors",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "instructor_categories_category_id_fkey",
                table: "instructor_categories",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "instructor_categories_instructors_id_fkey",
                table: "instructor_categories",
                column: "instructors_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "payments_student_id_fkey",
                table: "payments",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_instructor_id_fkey",
                table: "practice_sessions",
                column: "instructor_id",
                principalTable: "instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_student_id_fkey",
                table: "practice_sessions",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "practice_sessions_vehicle_plate_fkey",
                table: "practice_sessions",
                column: "vehicle_plate",
                principalTable: "vehicles",
                principalColumn: "plate_number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "students_group_id_fkey",
                table: "students",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "theory_sessions_group_id_fkey",
                table: "theory_sessions",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "theory_sessions_instructor_id_fkey",
                table: "theory_sessions",
                column: "instructor_id",
                principalTable: "instructors",
                principalColumn: "id");
        }
    }
}
