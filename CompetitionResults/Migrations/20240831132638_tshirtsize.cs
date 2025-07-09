using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class tshirtsize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TShirtSize",
                table: "Throwers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Throwers",
                keyColumn: "Id",
                keyValue: 1,
                column: "TShirtSize",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TShirtSize",
                table: "Throwers");
        }
    }
}
