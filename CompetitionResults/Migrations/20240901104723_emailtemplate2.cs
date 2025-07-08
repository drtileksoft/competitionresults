using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class emailtemplate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailTemplateFooterCZ",
                table: "Competitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Competitions",
                keyColumn: "Id",
                keyValue: 1,
                column: "EmailTemplateFooterCZ",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailTemplateFooterCZ",
                table: "Competitions");
        }
    }
}
