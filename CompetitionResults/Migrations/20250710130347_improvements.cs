using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class improvements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmailTemplateFooterCZ",
                table: "Competitions",
                newName: "EmailTemplateFooterLocal");

            migrationBuilder.AddColumn<int>(
                name: "CompetitionPriceEUR",
                table: "Competitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompetitionPriceLocal",
                table: "Competitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalLanguage",
                table: "Competitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LocalLanguage" },
                values: new object[] { "CZ" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitionPriceEUR",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "CompetitionPriceLocal",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "LocalLanguage",
                table: "Competitions");

            migrationBuilder.RenameColumn(
                name: "EmailTemplateFooterLocal",
                table: "Competitions",
                newName: "EmailTemplateFooterCZ");
        }
    }
}
