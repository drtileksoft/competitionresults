using System.Collections.Generic;
using System.Linq;
using CompetitionResults.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CompetitionResults.Data
{
	public class ResultService
	{
		private readonly CompetitionDbContext _context;
		private readonly NotificationHub _notificationHub;

		public ResultService(CompetitionDbContext context,
			NotificationHub notificationHub)
		{
			_context = context;
			_notificationHub = notificationHub;
		}

        public void CalculateAwardPoints(List<ResultDto> results, int maxAwardPoints, bool applyMissingResultPenalty)
        {
            double? previousPoints = null;
            int? previousBullseyes = null;
            int rank = 0;

            for (int i = 0; i < results.Count; i++)
            {
                var current = results[i];

                if (!current.Points.HasValue)
                {
                    current.PointsAward = applyMissingResultPenalty ? -10 : 0;
                    continue;
                }

                bool isTie = previousPoints.HasValue &&
                             current.Points == previousPoints &&
                             current.BullseyeCount == previousBullseyes;

                if (!isTie)
                    rank = i;

                current.PointsAward = maxAwardPoints - rank;

                previousPoints = current.Points;
                previousBullseyes = current.BullseyeCount;
            }
        }

        public void AssignPositions(List<ResultDto> results, bool isDividedToCategories, bool isReverseOrdered)
        {
            IEnumerable<IGrouping<int, ResultDto>> groups = isDividedToCategories
                ? results.GroupBy(r => r.CategoryId)
                : new[] { results.GroupBy(r => 0).FirstOrDefault() };

            foreach (var group in groups)
            {
                if (group == null)
                    continue;

                var sorted = OrderResults(group, isReverseOrdered);

                int actualRank = 0;
                int logicalRank = 0;
                double? prevPoints = null;
                int? prevBulls = null;

                foreach (var result in sorted)
                {
                    actualRank++;

                    bool isTie = prevPoints.HasValue &&
                                result.Points == prevPoints &&
                                result.BullseyeCount == prevBulls;

                    if (!isTie)
                        logicalRank = actualRank;

                    result.Position = logicalRank;
                    prevPoints = result.Points;
                    prevBulls = result.BullseyeCount;
                }
            }
        }

        public void MarkTiesForMedals(List<ResultDto> sortedResults, bool isDividedToCategories)
        {
            if (isDividedToCategories)
            {
                var categories = sortedResults.Select(r => r.CategoryId).Distinct();

                foreach (var categoryId in categories)
                {
                    var categoryResults = sortedResults
                        .Where(r => r.CategoryId == categoryId && r.Points.HasValue)
                        .ToList();

                    MarkTiesInTop3(categoryResults);
                }
            }
            else
            {
                var globalResults = sortedResults
                    .Where(r => r.Points.HasValue)
                    .ToList();

                MarkTiesInTop3(globalResults);
            }
        }

        private void MarkTiesInTop3(List<ResultDto> results)
        {
            int rank = 0;
            double? prevPoints = null;
            int? prevBulls = null;
            int logicalRank = 0;

            for (int i = 0; i < results.Count; i++)
            {
                var currentResult = results[i];

                bool isTie = prevPoints.HasValue &&
                             currentResult.Points == prevPoints &&
                             currentResult.BullseyeCount == prevBulls;

                if (!isTie)
                    logicalRank = rank + 1;

                if (logicalRank <= 3)
                {
                    // najdi všechny, kteří mají stejné points & bulls v rámci těchto top 3 pozic
                    var tiedGroup = results
                        .Where(x => x.Points == currentResult.Points && x.BullseyeCount == currentResult.BullseyeCount)
                        .ToList();

                    if (tiedGroup.Count > 1)
                    {
                        foreach (var tied in tiedGroup)
                            tied.IsTieForMedal = true;
                    }
                }

                rank++;
                prevPoints = currentResult.Points;
                prevBulls = currentResult.BullseyeCount;
            }
        }




        private static List<ResultDto> OrderResults(IEnumerable<ResultDto> results, bool isReverseOrdered)
        {
            if (results == null)
                return new List<ResultDto>();

            return isReverseOrdered
                ? results
                    .OrderBy(r => r.Points ?? double.MaxValue)
                    .ThenByDescending(r => r.BullseyeCount ?? -1)
                    .ToList()
                : results
                    .OrderByDescending(r => r.Points ?? double.MinValue)
                    .ThenByDescending(r => r.BullseyeCount ?? -1)
                    .ToList();
        }

        private List<ResultDto> RankResults(List<ResultDto> results, bool isDividedToCategories, bool isReverseOrdered, bool applyMissingResultPenalty)
        {
            var ordered = OrderResults(results, isReverseOrdered);

            if (ordered.Any())
            {
                CalculateAwardPoints(ordered, ordered.Count, applyMissingResultPenalty);
                AssignPositions(ordered, isDividedToCategories, isReverseOrdered);
                MarkTiesForMedals(ordered, isDividedToCategories);
            }

            return ordered;
        }

        private static List<ResultDto> BuildDisciplineResults(int disciplineId, IEnumerable<ThrowerInfo> throwers, IReadOnlyDictionary<(int DisciplineId, int ThrowerId), ResultProjection> existingResults)
        {
            var disciplineResults = new List<ResultDto>();

            foreach (var thrower in throwers)
            {
                existingResults.TryGetValue((disciplineId, thrower.Id), out var result);

                disciplineResults.Add(new ResultDto
                {
                    ThrowerId = thrower.Id,
                    DisciplineId = disciplineId,
                    ThrowerName = thrower.DisplayName,
                    CategoryId = thrower.CategoryId,
                    Points = result?.Points,
                    BullseyeCount = result?.BullseyeCount
                });
            }

            return disciplineResults;
        }

        private static string BuildThrowerDisplayName(string name, string surname, string? nickname)
        {
            if (!string.IsNullOrWhiteSpace(nickname))
                return $"{nickname} ({name} {surname})";

            return $"{name} {surname}".Trim();
        }

        private sealed class ThrowerInfo
        {
            public int Id { get; set; }

            public int? CategoryId { get; set; }

            public string DisplayName { get; set; } = string.Empty;
        }

        private sealed class ResultProjection
        {
            public int DisciplineId { get; set; }

            public int ThrowerId { get; set; }

            public double? Points { get; set; }

            public int? BullseyeCount { get; set; }
        }

        private sealed class DisciplineConfig
        {
            public int Id { get; set; }

            public bool HasPositionsInsteadPoints { get; set; }

            public bool IsDividedToCategories { get; set; }
        }

        public async Task<List<ResultDto>> GetRankedResultsAsync(int disciplineId, int competitionId)
        {
            var discipline = await _context.Disciplines
                .SingleAsync(d => d.Id == disciplineId && d.CompetitionId == competitionId);

            var isReverseOrdered = discipline.HasPositionsInsteadPoints;

            var throwers = await _context.Throwers
                .Where(t => t.CompetitionId == competitionId)
                .Select(t => new ThrowerInfo
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    DisplayName = BuildThrowerDisplayName(t.Name, t.Surname, t.Nickname)
                })
                .ToListAsync();

            var existingResults = await _context.Results
                .Where(r => r.DisciplineId == disciplineId && r.CompetitionId == competitionId)
                .Select(r => new ResultProjection
                {
                    DisciplineId = r.DisciplineId,
                    ThrowerId = r.ThrowerId,
                    Points = r.Points,
                    BullseyeCount = r.BullseyeCount
                })
                .ToDictionaryAsync(r => (r.DisciplineId, r.ThrowerId));

            var applyMissingResultPenalty = await _context.Competitions
                .Where(c => c.Id == competitionId)
                .Select(c => c.EnableMissingResultPenalty)
                .SingleAsync();

            var ranked = RankResults(
                BuildDisciplineResults(discipline.Id, throwers, existingResults),
                discipline.IsDividedToCategories,
                isReverseOrdered,
                applyMissingResultPenalty);

            return ranked;
        }





        public async Task<List<ResultDto>> GetResultsByCategoryAndDisciplineAsync(int categoryId, int disciplineId, int competitionId)
        {
            var allResults = await GetRankedResultsAsync(disciplineId, competitionId);
            return allResults
                .Where(r => r.CategoryId == categoryId)
                .ToList();
        }


        public async Task<List<ResultDto>> GetResultsByDisciplineAsync(int disciplineId, int competitionId)
        {
            return await GetRankedResultsAsync(disciplineId, competitionId);
        }


        public async Task<Dictionary<int, double?>> GetScoresByThrowerIdAsync(int throwerId, int competitionId)
		{
			// This method fetches scores for a given thrower
			var results = await _context.Results
										.Where(r => r.ThrowerId == throwerId && r.CompetitionId == competitionId)
										.ToListAsync();

			return results.ToDictionary(r => r.DisciplineId, r => r.Points);
		}


        public async Task<List<ResultDto>> GetResultsTotalAsync(int competitionId)
        {
            var disciplineConfigs = await _context.Disciplines
                .Where(d => d.CompetitionId == competitionId)
                .Select(d => new DisciplineConfig
                {
                    Id = d.Id,
                    HasPositionsInsteadPoints = d.HasPositionsInsteadPoints,
                    IsDividedToCategories = d.IsDividedToCategories
                })
                .ToListAsync();

            if (!disciplineConfigs.Any())
                return new List<ResultDto>();

            var throwers = await _context.Throwers
                .Where(t => t.CompetitionId == competitionId)
                .Select(t => new ThrowerInfo
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    DisplayName = BuildThrowerDisplayName(t.Name, t.Surname, t.Nickname)
                })
                .ToListAsync();

            if (!throwers.Any())
                return new List<ResultDto>();

            var resultLookup = await _context.Results
                .Where(r => r.CompetitionId == competitionId)
                .Select(r => new ResultProjection
                {
                    DisciplineId = r.DisciplineId,
                    ThrowerId = r.ThrowerId,
                    Points = r.Points,
                    BullseyeCount = r.BullseyeCount
                })
                .ToDictionaryAsync(r => (r.DisciplineId, r.ThrowerId));

            var applyMissingResultPenalty = await _context.Competitions
                .Where(c => c.Id == competitionId)
                .Select(c => c.EnableMissingResultPenalty)
                .SingleAsync();

            var totals = new Dictionary<int, ResultDto>();

            foreach (var discipline in disciplineConfigs)
            {
                var ranked = RankResults(
                    BuildDisciplineResults(discipline.Id, throwers, resultLookup),
                    discipline.IsDividedToCategories,
                    discipline.HasPositionsInsteadPoints,
                    applyMissingResultPenalty);

                foreach (var result in ranked.Where(r => r.PointsAward.HasValue))
                {
                    if (!totals.TryGetValue(result.ThrowerId, out var entry))
                    {
                        entry = new ResultDto
                        {
                            ThrowerId = result.ThrowerId,
                            ThrowerName = result.ThrowerName,
                            Points = 0
                        };
                        totals[result.ThrowerId] = entry;
                    }

                    entry.Points += result.PointsAward.Value;
                }
            }

            var orderedTotals = totals.Values
                .OrderByDescending(r => r.Points)
                .ThenBy(r => r.ThrowerName)
                .ToList();

            for (int i = 0; i < orderedTotals.Count; i++)
            {
                orderedTotals[i].Position = i + 1;
            }

            return orderedTotals;
        }

		public async Task DeleteResultsAsync(int competitionId)
		{
            // This method deletes all results for a given thrower
            var results = await _context.Results
                                        .Where(r => r.CompetitionId == competitionId)
                                        .ToListAsync();

            foreach (var result in results)
			{
                _context.Results.Remove(result);
            }

            await _context.SaveChangesAsync();

            await _notificationHub.NotifyCompetitionChanged();
        }

        public async Task FillRandomScoresAsync(int competitionId)
        {
            var throwers = await _context.Throwers.Where(t => t.CompetitionId == competitionId).ToListAsync();
            var disciplines = await _context.Disciplines.Where(d => d.CompetitionId == competitionId).ToListAsync();

            var random = new Random();

            foreach (var thrower in throwers)
            {
                foreach (var discipline in disciplines)
                {
                    double? score = null;

                    if (!discipline.HasPositionsInsteadPoints)
                    {
                        // Pokud se používají desetinová čísla
                        score = discipline.HasDecimalPoints
                            ? Math.Round(random.NextDouble() * 100, 2)
                            : random.Next(0, 101);
                    }
                    else
                    {
                        // Umístění 1–30
                        score = random.Next(1, 31);
                    }

                    var existing = await _context.Results
                        .FirstOrDefaultAsync(r =>
                            r.ThrowerId == thrower.Id &&
                            r.DisciplineId == discipline.Id &&
                            r.CompetitionId == competitionId);

                    if (existing != null)
                    {
                        existing.Points = score;
                    }
                    else
                    {
                        _context.Results.Add(new Results
                        {
                            CompetitionId = competitionId,
                            DisciplineId = discipline.Id,
                            ThrowerId = thrower.Id,
                            Points = score
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            await _notificationHub.NotifyCompetitionChanged();
        }

        public async Task<List<BullseyeEditEntry>> GetBullseyeEntriesAsync(int competitionId, int throwerId)
        {
            return await _context.Results
                .Where(r => r.CompetitionId == competitionId && r.ThrowerId == throwerId)
                .Include(r => r.Discipline)
                .Select(r => new BullseyeEditEntry
                {
                    DisciplineId = r.DisciplineId,
                    DisciplineName = r.Discipline.Name,
                    BullseyeCount = r.BullseyeCount
                }).ToListAsync();
        }

        public async Task SaveBullseyeEntriesAsync(int competitionId, int throwerId, List<BullseyeEditEntry> entries)
        {
            foreach (var entry in entries)
            {
                var result = await _context.Results
                    .FirstOrDefaultAsync(r =>
                        r.CompetitionId == competitionId &&
                        r.ThrowerId == throwerId &&
                        r.DisciplineId == entry.DisciplineId);

                if (result != null)
                {
                    result.BullseyeCount = entry.BullseyeCount;
                }
            }

            await _context.SaveChangesAsync();
            await _notificationHub.NotifyCompetitionChanged();
        }


        public async Task UpdateScoresAsync(int competitionId, int throwerId, Dictionary<int, double?> scores)
		{
			// This method updates the scores for a given thrower
			foreach (var score in scores)
			{
				var result = await _context.Results
										   .FirstOrDefaultAsync(r => r.ThrowerId == throwerId && r.DisciplineId == score.Key);

				if (result != null)
				{
					result.Points = score.Value;
				}
				else
				{
					// Create a new result if it doesn't exist
					_context.Results.Add(new Results
					{
						ThrowerId = throwerId,
						DisciplineId = score.Key,
						CompetitionId = competitionId,
						Points = score.Value
					});
				}
			}

			await _context.SaveChangesAsync();

			await _notificationHub.NotifyCompetitionChanged();
		}



        public async Task<List<NationMedalsDto>> GetMedalsByNationAsync(int competitionId)
        {
            var disciplines = await _context.Disciplines
                .Where(d => d.CompetitionId == competitionId)
                .ToListAsync();

            // Mapování throwerId → Nationality
            var throwerNationMap = await _context.Throwers
                .Where(t => t.CompetitionId == competitionId)
                .ToDictionaryAsync(
                    t => t.Id,
                    t => string.IsNullOrWhiteSpace(t.Nationality)
                        ? null
                        : t.Nationality.ToUpperInvariant());

            var nationMedals = new Dictionary<string, NationMedalsDto>();

            // 1️⃣ Zpracuj všechny disciplíny
            foreach (var discipline in disciplines)
            {
                var results = await GetResultsByDisciplineAsync(discipline.Id, competitionId);
                var top3 = results.Where(r => r.Position is >= 1 and <= 3);

                foreach (var result in top3)
                {
                    if (!throwerNationMap.TryGetValue(result.ThrowerId, out var nationality) ||
                        string.IsNullOrWhiteSpace(nationality))
                        continue;

                    if (!nationMedals.TryGetValue(nationality, out var entry))
                    {
                        entry = new NationMedalsDto { Nationality = nationality };
                        nationMedals[nationality] = entry;
                    }

                    switch (result.Position)
                    {
                        case 1: entry.Gold++; break;
                        case 2: entry.Silver++; break;
                        case 3: entry.Bronze++; break;
                    }
                }
            }

            // 2️⃣ Přidej Total Winner (disciplineId = 0 → GetResultsTotalAsync)
            var absoluteResults = await GetResultsTotalAsync(competitionId);
            var top3Total = absoluteResults.Take(3).ToList();
            if (top3Total.Count > 0) top3Total[0].Position = 1;
            if (top3Total.Count > 1) top3Total[1].Position = 2;
            if (top3Total.Count > 2) top3Total[2].Position = 3;


            foreach (var result in top3Total)
            {
                if (!throwerNationMap.TryGetValue(result.ThrowerId, out var nationality) ||
                    string.IsNullOrWhiteSpace(nationality))
                    continue;

                if (!nationMedals.TryGetValue(nationality, out var entry))
                {
                    entry = new NationMedalsDto { Nationality = nationality };
                    nationMedals[nationality] = entry;
                }

                switch (result.Position)
                {
                    case 1: entry.Gold++; break;
                    case 2: entry.Silver++; break;
                    case 3: entry.Bronze++; break;
                }
            }

            return nationMedals.Values
                .OrderByDescending(n => n.Total)
                .ThenByDescending(n => n.Gold)
                .ThenByDescending(n => n.Silver)
                .ThenByDescending(n => n.Bronze)
                .ToList();
        }



    }

    public class NationMedalsDto
    {
        public string Nationality { get; set; }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Bronze { get; set; }
        public int Total => Gold + Silver + Bronze;
    }

    public class NullableDoubleComparer : IComparer<double?>
	{
		public int Compare(double? x, double? y)
		{
			if (x == null && y == null)
				return 0;
			if (x == null)
				return 1;
			if (y == null)
				return -1;

			return x.Value.CompareTo(y.Value);
		}
	}

        public class ResultDto
        {
            public int ThrowerId { get; set; }
            public string ThrowerName { get; set; }
            public int Position { get; set; }
            public double? Points { get; set; }
            public int? BullseyeCount { get; set; }
            public double? PointsAward { get; set; }
            public bool IsTieForMedal { get; set; } // použito v UI k zobrazení červeného pozadí
            public int CategoryId { get; set; }
            public int DisciplineId { get; set; } // přidáno pro snadné filtrování

            public string BackgroundColor
            {
                get
                {
                    if (!Points.HasValue)
                    {
                        return "background-color: rgba(255,0,0,0.1);";
                    }

                    if (IsTieForMedal)
                    {
                        return "background-color: red;";
                    }

                    return Position switch
                    {
                        1 => "background-color: gold;",
                        2 => "background-color: silver;",
                        3 => "background-color: #CD7F32;",
                        _ => string.Empty
                    };
                }
            }
        }

}
