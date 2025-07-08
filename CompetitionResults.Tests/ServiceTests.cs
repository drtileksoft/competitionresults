using CompetitionResults.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace CompetitionResults.Tests;

public class ServiceTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public ServiceTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CategoryService_ReturnsCategories()
    {
        var categories = await _fixture.CategoryService.GetAllCategoriesAsync(1);
        Assert.True(categories.Count > 0);
    }

    [Fact]
    public async Task DisciplineService_ReturnsDisciplines()
    {
        var disciplines = await _fixture.DisciplineService.GetAllDisciplinesAsync(1);
        Assert.True(disciplines.Count > 0);
    }

    [Fact]
    public async Task CompetitionService_ReturnsCompetitions()
    {
        var comps = await _fixture.CompetitionService.GetAllCompetitionsAsync();
        Assert.True(comps.Count > 0);
    }

    [Fact]
    public async Task ThrowerService_ReturnsThrowers()
    {
        var throwers = await _fixture.ThrowerService.GetAllThrowersAsync(1);
        Assert.True(throwers.Count > 0);
    }

    [Fact]
    public async Task ResultService_ComputesPositionsAndAwards()
    {
        var competitionIds = await _fixture.Context.Competitions
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var competitionId in competitionIds)
        {
            var disciplines = await _fixture.Context.Disciplines
                .Where(d => d.CompetitionId == competitionId)
                .ToListAsync();

            foreach (var discipline in disciplines)
            {
                var results = await _fixture.Context.Results
                    .Include(r => r.Thrower)
                    .Where(r => r.DisciplineId == discipline.Id && r.CompetitionId == competitionId)
                    .Select(r => new ResultDto
                    {
                        ThrowerId = r.ThrowerId,
                        DisciplineId = r.DisciplineId,
                        ThrowerName = !string.IsNullOrEmpty(r.Thrower.Nickname)
                            ? r.Thrower.Nickname + " (" + r.Thrower.Name + " " + r.Thrower.Surname + ")"
                            : r.Thrower.Name + " " + r.Thrower.Surname,
                        CategoryId = r.Thrower.CategoryId,
                        Points = r.Points,
                        BullseyeCount = r.BullseyeCount
                    }).ToListAsync();

                var expected = discipline.HasPositionsInsteadPoints
                    ? results.OrderBy(r => r.Points ?? double.MaxValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList()
                    : results.OrderByDescending(r => r.Points ?? double.MinValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList();

                _fixture.ResultService.AssignPointsAwards(expected, expected.Count);
                _fixture.ResultService.AssignPositions(expected, discipline.IsDividedToCategories, discipline.HasPositionsInsteadPoints);
                _fixture.ResultService.MarkTiesForMedals(expected, discipline.IsDividedToCategories);

                var serviceResults = await _fixture.ResultService.GetResultsByDisciplineAsync(discipline.Id, competitionId);

                Assert.Equal(expected.Count, serviceResults.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i].ThrowerId, serviceResults[i].ThrowerId);
                    Assert.Equal(expected[i].Position, serviceResults[i].Position);
                    Assert.Equal(expected[i].PointsAward, serviceResults[i].PointsAward);
                    Assert.Equal(expected[i].IsTieForMedal, serviceResults[i].IsTieForMedal);
                }
            }
        }
    }
}
