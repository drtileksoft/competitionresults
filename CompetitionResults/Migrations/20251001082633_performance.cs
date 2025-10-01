using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class performance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_CompetitionId",
                table: "Results");

            migrationBuilder.CreateIndex(
                name: "IX_Translations_Key_LocalLanguage",
                table: "Translations",
                columns: new[] { "Key", "LocalLanguage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Translations_LocalLanguage",
                table: "Translations",
                column: "LocalLanguage");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CompetitionId_DisciplineId",
                table: "Results",
                columns: new[] { "CompetitionId", "DisciplineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Results_CompetitionId_ThrowerId",
                table: "Results",
                columns: new[] { "CompetitionId", "ThrowerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionManagers_ManagerId_CompetitionId",
                table: "CompetitionManagers",
                columns: new[] { "ManagerId", "CompetitionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Translations_Key_LocalLanguage",
                table: "Translations");

            migrationBuilder.DropIndex(
                name: "IX_Translations_LocalLanguage",
                table: "Translations");

            migrationBuilder.DropIndex(
                name: "IX_Results_CompetitionId_DisciplineId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_Results_CompetitionId_ThrowerId",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionManagers_ManagerId_CompetitionId",
                table: "CompetitionManagers");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CompetitionId",
                table: "Results",
                column: "CompetitionId");
        }
    }
}
