using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class payment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Payment",
                table: "Throwers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentDone",
                table: "Throwers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Throwers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Payment", "PaymentDone" },
                values: new object[] { null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payment",
                table: "Throwers");

            migrationBuilder.DropColumn(
                name: "PaymentDone",
                table: "Throwers");
        }
    }
}
