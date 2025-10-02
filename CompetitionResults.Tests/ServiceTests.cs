using CompetitionResults.Data;
using CompetitionResults.Constants;
using Xunit;

namespace CompetitionResults.Tests;

public class ServiceTests : IClassFixture<InMemoryDbFixture>
{
    private readonly InMemoryDbFixture _fixture;

    public ServiceTests(InMemoryDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CategoryService_ReturnsCategories()
    {
        var categories = await _fixture.CategoryService.GetAllCategoriesAsync(1);
        Assert.Equal(SeedData.Categories.Length, categories.Count);
    }

    [Fact]
    public async Task DisciplineService_ReturnsDisciplines()
    {
        var disciplines = await _fixture.DisciplineService.GetAllDisciplinesAsync(1);
        Assert.Equal(SeedData.Disciplines.Length, disciplines.Count);
    }

    [Fact]
    public async Task CompetitionService_ReturnsCompetitions()
    {
        var comps = await _fixture.CompetitionService.GetAllCompetitionsAsync();
        Assert.Equal(SeedData.Competitions.Length, comps.Count);
    }

    [Fact]
    public async Task ThrowerService_ReturnsThrowers()
    {
        var throwers = await _fixture.ThrowerService.GetAllThrowersAsync(1);
        Assert.Equal(SeedData.Throwers.Length, throwers.Count);
    }

    [Fact]
    public async Task ResultService_ReturnsExpectedResults_ForEachDiscipline()
    {
        foreach (var discipline in SeedData.Disciplines)
        {
            var serviceResults = await _fixture.ResultService.GetResultsByDisciplineAsync(discipline.Id, 1);
            var expected = ExpectedResults.ByDiscipline[discipline.Id];

            Assert.Equal(expected.Count, serviceResults.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].ThrowerId, serviceResults[i].ThrowerId);
                Assert.Equal(expected[i].Position, serviceResults[i].Position);
                Assert.Equal(expected[i].PointsAward, serviceResults[i].PointsAward);
                Assert.Equal(expected[i].IsTieForMedal, serviceResults[i].IsTieForMedal);
                Assert.Equal(expected[i].ThrowerName, serviceResults[i].ThrowerName);
            }
        }
    }

    [Fact]
    public async Task ResultService_ReturnsExpectedOverallResults()
    {
        var overall = await _fixture.ResultService.GetResultsTotalAsync(1);
        var expected = ExpectedResults.Overall;

        Assert.Equal(expected.Count, overall.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].ThrowerId, overall[i].ThrowerId);
            Assert.Equal(expected[i].ThrowerName, overall[i].ThrowerName);
            Assert.Equal(expected[i].Points, overall[i].Points);
        }
    }

    [Fact]
    public async Task AddCompetition_AddsDefaultTranslations()
    {
        var comp = new Competition { Name = "Test", LocalLanguage = "DE" };
        await _fixture.CompetitionService.AddCompetitionAsync(comp);

        var translations = await _fixture.TranslationService.GetTranslationsByLanguageAsync("DE");
        Assert.Equal(TranslationKeys.All.Length, translations.Count);
        Assert.Contains(translations, t => t.Key == TranslationKeys.Name);
    }

    [Fact]
    public async Task GetMedalsByNation_SkipsThrowersWithoutNationality()
    {
        var competition = new Competition { Id = 999, Name = "Medals Test", LocalLanguage = "EN" };
        var category = new Category { Id = 999, Name = "Test", CompetitionId = competition.Id };
        var discipline = new Discipline
        {
            Id = 999,
            Name = "Primary",
            CompetitionId = competition.Id,
            HasPositionsInsteadPoints = false,
            HasDecimalPoints = false,
            IsDividedToCategories = false
        };

        var throwerWithNationality = new Thrower
        {
            Id = 999,
            CompetitionId = competition.Id,
            CategoryId = category.Id,
            Name = "Alex",
            Surname = "Nation",
            Nationality = "GB"
        };

        var throwerWithoutNationality = new Thrower
        {
            Id = 1000,
            CompetitionId = competition.Id,
            CategoryId = category.Id,
            Name = "Jamie",
            Surname = "Unknown",
            Nationality = null
        };

        _fixture.Context.Competitions.Add(competition);
        _fixture.Context.Categories.Add(category);
        _fixture.Context.Disciplines.Add(discipline);
        _fixture.Context.Throwers.AddRange(throwerWithNationality, throwerWithoutNationality);
        _fixture.Context.Results.AddRange(
            new Results
            {
                CompetitionId = competition.Id,
                DisciplineId = discipline.Id,
                ThrowerId = throwerWithNationality.Id,
                Points = 50
            },
            new Results
            {
                CompetitionId = competition.Id,
                DisciplineId = discipline.Id,
                ThrowerId = throwerWithoutNationality.Id,
                Points = 40
            });

        await _fixture.Context.SaveChangesAsync();

        var medals = await _fixture.ResultService.GetMedalsByNationAsync(competition.Id);

        Assert.All(medals, m => Assert.False(string.IsNullOrWhiteSpace(m.Nationality)));
        Assert.Contains(medals, m => m.Nationality == "GB");
    }
}
