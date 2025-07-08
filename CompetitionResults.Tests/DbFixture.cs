using CompetitionResults.Data;
using CompetitionResults.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CompetitionResults.Tests;

public class DbFixture : IDisposable
{
    public CompetitionDbContext Context { get; }
    public ResultService ResultService { get; }
    public CategoryService CategoryService { get; }
    public DisciplineService DisciplineService { get; }
    public CompetitionService CompetitionService { get; }
    public ThrowerService ThrowerService { get; }

    public DbFixture()
    {
        var options = new DbContextOptionsBuilder<CompetitionDbContext>()
            .UseSqlite("Data Source=competition.db")
            .Options;
        Context = new CompetitionDbContext(options);

        var notificationHub = new NotificationHub(null!);
        ResultService = new ResultService(Context, notificationHub);
        CategoryService = new CategoryService(Context);
        DisciplineService = new DisciplineService(Context);
        CompetitionService = new CompetitionService(Context);
        ThrowerService = new ThrowerService(Context, notificationHub, new ConfigurationBuilder().Build(), new FakeEmailSender());
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

internal class FakeEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
}
