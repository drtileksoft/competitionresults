using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class startnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StartingNumber",
                table: "Throwers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Throwers",
                keyColumn: "Id",
                keyValue: 1,
                column: "StartingNumber",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartingNumber",
                table: "Throwers");
        }
    }
}
