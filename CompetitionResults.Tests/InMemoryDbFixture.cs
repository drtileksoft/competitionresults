using CompetitionResults.Data;
using CompetitionResults.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CompetitionResults.Tests;

public class InMemoryDbFixture : IDisposable
{
    public CompetitionDbContext Context { get; }
    public ResultService ResultService { get; }
    public CategoryService CategoryService { get; }
    public DisciplineService DisciplineService { get; }
    public CompetitionService CompetitionService { get; }
    public ThrowerService ThrowerService { get; }
    public TranslationService TranslationService { get; }

    public InMemoryDbFixture()
    {
        var options = new DbContextOptionsBuilder<CompetitionDbContext>()
            .UseInMemoryDatabase("CompetitionTestDb")
            .Options;
        Context = new CompetitionDbContext(options);

        Context.Competitions.AddRange(SeedData.Competitions);
        Context.Categories.AddRange(SeedData.Categories);
        Context.Disciplines.AddRange(SeedData.Disciplines);
        Context.Throwers.AddRange(SeedData.Throwers);
        Context.Results.AddRange(SeedData.Results);
        Context.Translations.AddRange(new[]
        {
            new Translation { Id = 1, Key = "Camping on site", LocalLanguage = "CZ", Value = "Kempování na místě" },
            new Translation { Id = 2, Key = "Want T-Shirt", LocalLanguage = "CZ", Value = "Chci tričko" },
            new Translation { Id = 3, Key = "T-Shirt Size", LocalLanguage = "CZ", Value = "Velikost trička" },
            new Translation { Id = 4, Key = "Registration for competition", LocalLanguage = "CZ", Value = "Registrace do závodu" },
            new Translation { Id = 5, Key = "General Announcement", LocalLanguage = "CZ", Value = "Obecná zpráva" },
            new Translation { Id = 6, Key = "Important - Payment for competition", LocalLanguage = "CZ", Value = "Dulezite - Platba za registraci" },
            new Translation { Id = 7, Key = "You have been successfully registered to competition:", LocalLanguage = "CZ", Value = "Byl/a jste úspěšně registrován/a na soutěž:" },
            new Translation { Id = 8, Key = "Name", LocalLanguage = "CZ", Value = "Jméno" },
            new Translation { Id = 9, Key = "Surname", LocalLanguage = "CZ", Value = "Příjmení" },
            new Translation { Id = 10, Key = "Nickname", LocalLanguage = "CZ", Value = "Přezdívka" },
            new Translation { Id = 11, Key = "Nationality", LocalLanguage = "CZ", Value = "Národnost" },
            new Translation { Id = 12, Key = "Club name", LocalLanguage = "CZ", Value = "Jméno klubu" },
            new Translation { Id = 13, Key = "Email", LocalLanguage = "CZ", Value = "Email" },
            new Translation { Id = 14, Key = "Note", LocalLanguage = "CZ", Value = "Poznámka" },
            new Translation { Id = 15, Key = "Category", LocalLanguage = "CZ", Value = "Kategorie" },
            new Translation { Id = 16, Key = "Hello,", LocalLanguage = "CZ", Value = "Dobrý den," },
            new Translation { Id = 17, Key = "This email is automatically generated because you have registered for the competition and have not yet paid.", LocalLanguage = "CZ", Value = "Tento email je automaticky generován, protože jste se zaregistrovali na soutěž a ještě jste nezaplatili." },
            new Translation { Id = 18, Key = "The limit for the number of participants has been set to {0}. Registration is final only after payment.", LocalLanguage = "CZ", Value = "Limit pro počet účastníků byl nastaven na {0}. Registrace je finální až po zaplacení." },
            new Translation { Id = 19, Key = "Currently, {0} out of {1} participants have paid.", LocalLanguage = "CZ", Value = "Aktuálně má zaplaceno {0} z {1} účastníků." },
            new Translation { Id = 20, Key = "Please pay as soon as possible, otherwise someone else will be faster than you and you will not be able to participate in the competition.", LocalLanguage = "CZ", Value = "Prosím, zaplaťte co nejdříve, jinak Vás předběhne někdo jiný a nebudete se moci zúčastnit soutěže." },
            new Translation { Id = 21, Key = "Thank you.", LocalLanguage = "CZ", Value = "Děkujeme." },
            new Translation { Id = 22, Key = "Team {0}", LocalLanguage = "CZ", Value = "Tým {0}" }
        });
        Context.SaveChanges();

        var notificationHub = new NotificationHub(null!);
        TranslationService = new TranslationService(Context);
        ResultService = new ResultService(Context, notificationHub);
        CategoryService = new CategoryService(Context);
        DisciplineService = new DisciplineService(Context);
        CompetitionService = new CompetitionService(Context);
        ThrowerService = new ThrowerService(Context, notificationHub, new ConfigurationBuilder().Build(), TranslationService);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

