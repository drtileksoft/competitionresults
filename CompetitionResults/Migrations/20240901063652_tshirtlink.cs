using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class tshirtlink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CampingOnSiteAvailable",
                table: "Competitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TShirtAvailable",
                table: "Competitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TShirtLink",
                table: "Competitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CampingOnSiteAvailable", "TShirtAvailable", "TShirtLink" },
                values: new object[] { false, false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampingOnSiteAvailable",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "TShirtAvailable",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "TShirtLink",
                table: "Competitions");
        }
    }
}
