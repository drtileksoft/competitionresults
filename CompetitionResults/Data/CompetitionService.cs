using CompetitionResults.Backup;
using CompetitionResults.Constants;
using Microsoft.EntityFrameworkCore;

namespace CompetitionResults.Data
{
    public class CompetitionService
    {
        private readonly CompetitionDbContext _context;

        public event Action OnCompetitionsChanged;

        private void NotifyCompetitionsChanged() => OnCompetitionsChanged?.Invoke();

        public CompetitionService(CompetitionDbContext context)
        {
            _context = context;
        }

        // Get all competitions
        public async Task<List<Competition>> GetAllCompetitionsAsync()
        {
            return await _context.Competitions.ToListAsync();
        }

        public async Task<List<Competition>> GetCompetitionsForManagerAsync(string managerId)
        {
            //var count = _context.Throwers.CountAsync();

            return await _context.Competitions
                .Where(c => c.CompetitionManagers.Any(cm => cm.ManagerId == managerId))
                .ToListAsync();
        }

        public async Task AssignManagerToCompetitionAsync(string userId, int competitionId)
        {
            var alreadyAssigned = _context.CompetitionManagers.Any(cm => cm.ManagerId == userId && cm.CompetitionId == competitionId);
            if (!alreadyAssigned)
            {
                _context.CompetitionManagers.Add(new CompetitionManager { ManagerId = userId, CompetitionId = competitionId });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveManagerFromCompetitionAsync(string userId, int competitionId)
        {
            var assignment = await _context.CompetitionManagers
                .FirstOrDefaultAsync(cm => cm.ManagerId == userId && cm.CompetitionId == competitionId);
            if (assignment != null)
            {
                _context.CompetitionManagers.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ApplicationUser>> GetManagersForCompetitionAsync(int competitionId)
        {
            return await _context.CompetitionManagers
                .Where(cm => cm.CompetitionId == competitionId)
                .Select(cm => cm.Manager)
                .ToListAsync();
        }

        // Get a single competition by ID
        public async Task<Competition> GetCompetitionByIdAsync(int id)
        {
            return await _context.Competitions.FindAsync(id);
        }

        // Add a new competition
        public async Task AddCompetitionAsync(Competition competition)
        {
            _context.Competitions.Add(competition);
            await _context.SaveChangesAsync();

            foreach (var key in TranslationKeys.All)
            {
                if (!_context.Translations.Any(t => t.Key == key && t.LocalLanguage == competition.LocalLanguage))
                {
                    _context.Translations.Add(new Translation { Key = key, LocalLanguage = competition.LocalLanguage, Value = key });
                }
            }
            await _context.SaveChangesAsync();

            NotifyCompetitionsChanged();
        }

        // Update an existing competition
        public async Task UpdateCompetitionAsync(Competition competition)
        {
            _context.Competitions.Update(competition);
            await _context.SaveChangesAsync();

            foreach (var key in TranslationKeys.All)
            {
                if (!_context.Translations.Any(t => t.Key == key && t.LocalLanguage == competition.LocalLanguage))
                {
                    _context.Translations.Add(new Translation { Key = key, LocalLanguage = competition.LocalLanguage, Value = key });
                }
            }
            await _context.SaveChangesAsync();

            NotifyCompetitionsChanged();
        }

        // Delete a competition
        public async Task DeleteCompetitionAsync(int id)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition != null)
            {
                _context.Competitions.Remove(competition);
                await _context.SaveChangesAsync();
                NotifyCompetitionsChanged();
            }
        }

        // Get backups
        public async Task<string> BackupAllAsync()
        {
            try
            {
                await BackupTools.BackupDatabaseAsync();

                var now = DateTime.Now;
                var roundedTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - now.Minute % 10, 0);
                var backupFilePath = $"./backup-simple_{roundedTime:yyyyMMddHHmm}.json";
                await DbContextExtensions.ExportSimpleDataToJsonAsync(_context, backupFilePath);
                return await DbContextExtensions.ExportDataToJsonAsync(_context, backupFilePath);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task RestoreAllAsync(string jsonData)
        {
            // Write the jsonData to a temporary file or handle it directly
            var tempFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFilePath, jsonData);

            // Use the existing import method
            await DbContextExtensions.ImportDataFromJsonAsync(_context, tempFilePath);

            // Clean up temporary file
            File.Delete(tempFilePath);
        }

        // Get backup
        public async Task<string> BackupCompetitionAsync(int competitionId)
        {
            try
            {
                return await DbContextExtensions.ExportCompetitionToJsonAsync(_context, competitionId);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task RestoreCompetitionAsync(string jsonData)
        {
            // Write the jsonData to a temporary file or handle it directly
            var tempFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFilePath, jsonData);

            // Use the existing import method
            await DbContextExtensions.ImportCompetitionAsNewAsync(_context, tempFilePath);

            // Clean up temporary file
            File.Delete(tempFilePath);
        }

        public async Task ClearDBAsync()
        {
            await DbContextExtensions.ClearDBAsync(_context);
        }
    }
}
