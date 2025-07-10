using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CompetitionResults.Migrations
{
    /// <inheritdoc />
    public partial class translations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { 1, "Camping on site", "Kempování na místě" },
                    { 2, "Want T-Shirt", "Chci tričko" },
                    { 3, "T-Shirt Size", "Velikost trička" },
                    { 4, "Registration for competition", "Registrace do závodu" },
                    { 5, "General Announcement", "Obecná zpráva" },
                    { 6, "Important - Payment for competition", "Dulezite - Platba za registraci" },
                    { 7, "You have been successfully registered to competition:", "Byl/a jste úspěšně registrován/a na soutěž:" },
                    { 8, "Name", "Jméno" },
                    { 9, "Surname", "Příjmení" },
                    { 10, "Nickname", "Přezdívka" },
                    { 11, "Nationality", "Národnost" },
                    { 12, "Club name", "Jméno klubu" },
                    { 13, "Email", "Email" },
                    { 14, "Note", "Poznámka" },
                    { 15, "Category", "Kategorie" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Translations");
        }
    }
}
