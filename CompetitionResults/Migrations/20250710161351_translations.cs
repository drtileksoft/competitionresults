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
                    LocalLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Key", "LocalLanguage", "Value" },
                values: new object[,]
                {
                    { 1, "Camping on site", "CZ", "Kempování na místě" },
                    { 2, "Want T-Shirt", "CZ", "Chci tričko" },
                    { 3, "T-Shirt Size", "CZ", "Velikost trička" },
                    { 4, "Registration for competition", "CZ", "Registrace do závodu" },
                    { 5, "General Announcement", "CZ", "Obecná zpráva" },
                    { 6, "Important - Payment for competition", "CZ", "Dulezite - Platba za registraci" },
                    { 7, "You have been successfully registered to competition:", "CZ", "Byl/a jste úspěšně registrován/a na soutěž:" },
                    { 8, "Name", "CZ", "Jméno" },
                    { 9, "Surname", "CZ", "Příjmení" },
                    { 10, "Nickname", "CZ", "Přezdívka" },
                    { 11, "Nationality", "CZ", "Národnost" },
                    { 12, "Club name", "CZ", "Jméno klubu" },
                    { 13, "Email", "CZ", "Email" },
                    { 14, "Note", "CZ", "Poznámka" },
                    { 15, "Category", "CZ", "Kategorie" },
                    { 16, "Hello,", "CZ", "Dobrý den," },
                    { 17, "This email is automatically generated because you have registered for the competition and have not yet paid.", "CZ", "Tento email je automaticky generován, protože jste se zaregistrovali na soutěž a ještě jste nezaplatili." },
                    { 18, "The limit for the number of participants has been set to {0}. Registration is final only after payment.", "CZ", "Limit pro počet účastníků byl nastaven na {0}. Registrace je finální až po zaplacení." },
                    { 19, "Currently, {0} out of {1} participants have paid.", "CZ", "Aktuálně má zaplaceno {0} z {1} účastníků." },
                    { 20, "Please pay as soon as possible, otherwise someone else will be faster than you and you will not be able to participate in the competition.", "CZ", "Prosím, zaplaťte co nejdříve, jinak Vás předběhne někdo jiný a nebudete se moci zúčastnit soutěže." },
                    { 21, "Thank you.", "CZ", "Děkujeme." },
                    { 22, "Team {0}", "CZ", "Tým {0}" }
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
