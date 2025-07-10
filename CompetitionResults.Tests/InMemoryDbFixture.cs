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
        Context.SaveChanges();

        var notificationHub = new NotificationHub(null!);
        ResultService = new ResultService(Context, notificationHub);
        CategoryService = new CategoryService(Context);
        DisciplineService = new DisciplineService(Context);
        CompetitionService = new CompetitionService(Context);
        ThrowerService = new ThrowerService(Context, notificationHub, new ConfigurationBuilder().Build());
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

