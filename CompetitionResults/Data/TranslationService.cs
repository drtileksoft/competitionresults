using Microsoft.EntityFrameworkCore;

namespace CompetitionResults.Data
{
    public class TranslationService
    {
        private readonly CompetitionDbContext _context;

        public TranslationService(CompetitionDbContext context)
        {
            _context = context;
        }

        public async Task<List<Translation>> GetTranslationsByLanguageAsync(string language)
        {
            return await _context.Translations
                .Where(t => t.LocalLanguage == language)
                .ToListAsync();
        }

        public async Task AddOrUpdateTranslationAsync(Translation translation)
        {
            if (translation.Id == 0)
            {
                _context.Translations.Add(translation);
            }
            else
            {
                _context.Translations.Update(translation);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTranslationAsync(int id)
        {
            var tr = await _context.Translations.FindAsync(id);
            if (tr != null)
            {
                _context.Translations.Remove(tr);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetValueAsync(string key, string language)
        {
            var tr = await _context.Translations.FirstOrDefaultAsync(t => t.Key == key && t.LocalLanguage == language);
            return tr?.Value ?? key;
        }
    }
}
