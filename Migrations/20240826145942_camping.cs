using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class camping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "IsCampingOnSite",
                table: "Throwers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Your competition name");

            migrationBuilder.UpdateData(
                table: "Throwers",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsCampingOnSite",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCampingOnSite",
                table: "Throwers");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CompetitionId", "Name" },
                values: new object[,]
                {
                    { 3, 1, "Amateur" },
                    { 4, 1, "Junior" }
                });

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Blade Throwers z. s. Championship 2024");

            migrationBuilder.InsertData(
                table: "Competitions",
                columns: new[] { "Id", "Name" },
                values: new object[] { 2, "U.K.A.T World Cup 202X" });

            migrationBuilder.InsertData(
                table: "Disciplines",
                columns: new[] { "Id", "CompetitionId", "HasDecimalPoints", "HasPositionsInsteadPoints", "IsDividedToCategories", "Name" },
                values: new object[,]
                {
                    { 13, 1, false, false, false, "Hunter" },
                    { 14, 1, false, false, false, "Moving Target" },
                    { 15, 1, false, false, false, "Plank" },
                    { 16, 1, false, false, false, "Horse" },
                    { 17, 1, false, false, false, "Criss Cross" },
                    { 18, 1, false, false, false, "Throw Everything" },
                    { 19, 1, false, false, true, "Double Bit Axe" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CompetitionId", "Name" },
                values: new object[,]
                {
                    { 5, 2, "Men" },
                    { 6, 2, "Women" }
                });

            migrationBuilder.InsertData(
                table: "Disciplines",
                columns: new[] { "Id", "CompetitionId", "HasDecimalPoints", "HasPositionsInsteadPoints", "IsDividedToCategories", "Name" },
                values: new object[,]
                {
                    { 20, 2, false, false, true, "Walkback Nospin" },
                    { 21, 2, false, false, true, "Walkback Knife" },
                    { 22, 2, false, false, true, "Walkback Axe" },
                    { 23, 2, true, false, true, "Long Distance Nospin" },
                    { 24, 2, true, false, true, "Long Distance Knife" },
                    { 25, 2, true, false, true, "Long Distance Axe" },
                    { 26, 2, false, false, false, "Silhouette Knife" },
                    { 27, 2, false, false, false, "Silhouette Axe" },
                    { 28, 2, false, true, false, "Coutanque" },
                    { 29, 2, false, true, false, "Duel" }
                });
        }
    }
}
