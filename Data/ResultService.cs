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

        public void AssignPointsAwards(List<ResultDto> results, int maxAwardValue)
        {
            double? previousPoints = null;
            int? previousBullseyes = null;
            int rank = 0;

            for (int i = 0; i < results.Count; i++)
            {
                var current = results[i];

                if (!current.Points.HasValue)
                {
                    current.PointsAward = -10;
                    continue;
                }

                bool isTie = previousPoints.HasValue &&
                             current.Points == previousPoints &&
                             current.BullseyeCount == previousBullseyes;

                if (!isTie)
                    rank = i;

                current.PointsAward = maxAwardValue - rank;

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
                var sorted = isReverseOrdered
                    ? group.OrderBy(r => r.Points ?? double.MaxValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList()
                    : group.OrderByDescending(r => r.Points ?? double.MinValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList();

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
                var r = results[i];

                bool isTie = prevPoints.HasValue &&
                             r.Points == prevPoints &&
                             r.BullseyeCount == prevBulls;

                if (!isTie)
                    logicalRank = rank + 1;

                if (logicalRank <= 3)
                {
                    // najdi všechny, kteří mají stejné points & bulls v rámci těchto top 3 pozic
                    var tiedGroup = results
                        .Where(x => x.Points == r.Points && x.BullseyeCount == r.BullseyeCount)
                        .ToList();

                    if (tiedGroup.Count > 1)
                    {
                        foreach (var tied in tiedGroup)
                            tied.IsTieForMedal = true;
                    }
                }

                rank++;
                prevPoints = r.Points;
                prevBulls = r.BullseyeCount;
            }
        }



        public async Task<List<ResultDto>> GetMixedRankedResultsAsync(int disciplineId, int competitionId)
        {
            var discipline = await _context.Disciplines
                .SingleAsync(d => d.Id == disciplineId && d.CompetitionId == competitionId);

            var isReverseOrdered = discipline.HasPositionsInsteadPoints;

            var results = await _context.Results
                .Include(r => r.Thrower)
                .Where(r => r.DisciplineId == disciplineId && r.CompetitionId == competitionId)
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

            // 🔁 Řazení zde – jen jednou, použije se i na awards i na medaile
            results = isReverseOrdered
                ? results.OrderBy(r => r.Points ?? double.MaxValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList()
                : results.OrderByDescending(r => r.Points ?? double.MinValue).ThenByDescending(r => r.BullseyeCount ?? -1).ToList();

            AssignPointsAwards(results, results.Count);
            AssignPositions(results, discipline.IsDividedToCategories, isReverseOrdered);
            MarkTiesForMedals(results, discipline.IsDividedToCategories);

            return results;
        }





        public async Task<List<ResultDto>> GetResultsByCategoryAndDisciplineAsync(int categoryId, int disciplineId, int competitionId)
        {
            var allResults = await GetMixedRankedResultsAsync(disciplineId, competitionId);
            return allResults
                .Where(r => r.CategoryId == categoryId)
                .ToList();
        }


        public async Task<List<ResultDto>> GetResultsByDisciplineAsync(int disciplineId, int competitionId)
        {
            return await GetMixedRankedResultsAsync(disciplineId, competitionId);
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
			// Fetch all disciplines for the given competition
			var disciplines = await _context.Disciplines
				.Where(c => c.CompetitionId == competitionId)
				.ToListAsync();

			// Dictionary to hold total points for each thrower
			var totalPoints = new Dictionary<string, double>();

            var throwerIds = new Dictionary<string, int>();

            foreach (var discipline in disciplines)
			{
				var results = await GetResultsByDisciplineAsync(discipline.Id, competitionId);

				// Assign points based on rank
				for (int i = 0; i < results.Count; i++)
				{
					var throwerId = results[i].ThrowerName;

					if (results[i].PointsAward.HasValue)
					{
						if (totalPoints.ContainsKey(throwerId))
						{
							totalPoints[throwerId] += results[i].PointsAward.Value;
                        }
						else
						{
							totalPoints.Add(throwerId, results[i].PointsAward.Value);

                            throwerIds.Add(throwerId, results[i].ThrowerId); // Store the throwerId for this throwerName
                        }
					}
				}
			}

			// Convert the dictionary to a list of ResultDto
			var resultList = new List<ResultDto>();
			foreach (var entry in totalPoints)
			{
				resultList.Add(new ResultDto
                {
                    ThrowerId = throwerIds[entry.Key],
                    ThrowerName = entry.Key,
					Points = entry.Value
				});
			}

			// Sort the result list by points in descending order to get the highest scoring thrower first
			resultList = resultList.OrderByDescending(r => r.Points).ToList();

			return resultList;
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
                .ToDictionaryAsync(t => t.Id, t => t.Nationality.ToUpper());

            var nationMedals = new Dictionary<string, NationMedalsDto>();

            // 1️⃣ Zpracuj všechny disciplíny
            foreach (var discipline in disciplines)
            {
                var results = await GetResultsByDisciplineAsync(discipline.Id, competitionId);
                var top3 = results.Where(r => r.Position is >= 1 and <= 3);

                foreach (var result in top3)
                {
                    if (!throwerNationMap.TryGetValue(result.ThrowerId, out var nationality))
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
                if (!throwerNationMap.TryGetValue(result.ThrowerId, out var nationality))
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

            // 3️⃣ Přidej ručně medaile za speciální disciplínu mimo systém
            void AddMedals(string nationality, int gold, int silver, int bronze)
            {
                nationality = nationality.ToUpper();

                if (!nationMedals.TryGetValue(nationality, out var entry))
                {
                    entry = new NationMedalsDto { Nationality = nationality };
                    nationMedals[nationality] = entry;
                }

                entry.Gold += gold;
                entry.Silver += silver;
                entry.Bronze += bronze;
            }

            // 🟨 Slovensko: 2 medaile (např. 1 gold + 1 bronze)
            AddMedals("SK", gold: 1, silver: 1, bronze: 0);

            // 🟥 Česko: 1 medaile (např. 1 silver)
            AddMedals("CZ", gold: 0, silver: 0, bronze: 1);

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
	}

}
