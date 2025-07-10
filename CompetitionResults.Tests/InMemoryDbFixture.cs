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
            new Translation { Id = 1, Key = "Camping on site", Value = "Kempování na místě" }
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

