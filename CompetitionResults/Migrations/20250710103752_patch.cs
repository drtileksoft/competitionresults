using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class patch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.UpdateData(
            //    table: "Throwers",
            //    keyColumn: "Id",
            //    keyValue: 1,
            //    column: "StartingNumber",
            //    value: 1);

            migrationBuilder.Sql(@"UPDATE Throwers
            SET StartingNumber = (
                SELECT COUNT(*) FROM Throwers t2
                WHERE t2.CompetitionId = Throwers.CompetitionId
                  AND t2.Id <= Throwers.Id
            );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Throwers",
                keyColumn: "Id",
                keyValue: 1,
                column: "StartingNumber",
                value: 0);
        }
    }
}
