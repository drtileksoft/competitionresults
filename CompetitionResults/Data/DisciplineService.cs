using Microsoft.EntityFrameworkCore;

namespace CompetitionResults.Data
{
    public class DisciplineService
    {
        private readonly CompetitionDbContext _context;

        public DisciplineService(CompetitionDbContext context)
        {
            _context = context;
        }

        public async Task<List<Discipline>> GetAllDisciplinesAsync(int competitionId)
        {
            return await _context.Disciplines.Where(c => c.CompetitionId == competitionId).ToListAsync();
		}

        public async Task<Discipline> GetDisciplineByIdAsync(int id)
        {
            return await _context.Disciplines.FindAsync(id);
        }

        public async Task AddDisciplineAsync(Discipline discipline)
        {
            _context.Disciplines.Add(discipline);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDisciplineAsync(Discipline discipline)
        {
            _context.Disciplines.Update(discipline);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDisciplineAsync(int id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline != null)
            {
                _context.Disciplines.Remove(discipline);
                await _context.SaveChangesAsync();
            }
        }
    }
}
