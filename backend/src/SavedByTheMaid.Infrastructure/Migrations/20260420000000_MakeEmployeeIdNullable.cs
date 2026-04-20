using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SavedByTheMaid.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260420000000_MakeEmployeeIdNullable")]
    public partial class MakeEmployeeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK constraints before altering columns
            migrationBuilder.DropForeignKey(
                name: "FK_SlotOccupancies_Employees_EmployeeId",
                table: "SlotOccupancies");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftReserves_Employees_EmployeeId",
                table: "SoftReserves");

            // Make EmployeeId nullable in SlotOccupancies
            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "SlotOccupancies",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false);

            // Make EmployeeId nullable in SoftReserves
            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "SoftReserves",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false);

            // Recreate FKs as optional (no cascade delete on null)
            migrationBuilder.AddForeignKey(
                name: "FK_SlotOccupancies_Employees_EmployeeId",
                table: "SlotOccupancies",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftReserves_Employees_EmployeeId",
                table: "SoftReserves",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlotOccupancies_Employees_EmployeeId",
                table: "SlotOccupancies");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftReserves_Employees_EmployeeId",
                table: "SoftReserves");

            // Revert to non-nullable (only safe if no nulls exist in data)
            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "SlotOccupancies",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "SoftReserves",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotOccupancies_Employees_EmployeeId",
                table: "SlotOccupancies",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftReserves_Employees_EmployeeId",
                table: "SoftReserves",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
