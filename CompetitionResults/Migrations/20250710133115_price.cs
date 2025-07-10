using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class price : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TShirtPriceEUR",
                table: "Competitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TShirtPriceLocal",
                table: "Competitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CompetitionPriceEUR", "CompetitionPriceLocal", "TShirtPriceEUR", "TShirtPriceLocal" },
                values: new object[] { 90, 2200, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TShirtPriceEUR",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "TShirtPriceLocal",
                table: "Competitions");

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CompetitionPriceEUR", "CompetitionPriceLocal" },
                values: new object[] { null, null });
        }
    }
}
