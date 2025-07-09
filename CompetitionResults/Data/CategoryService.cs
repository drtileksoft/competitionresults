using Microsoft.EntityFrameworkCore;

namespace CompetitionResults.Data
{
    public class CategoryService
    {
        private readonly CompetitionDbContext _context;

        public CategoryService(CompetitionDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync(int competitionId)
        {
            return await _context.Categories.Where(c => c.CompetitionId == competitionId).ToListAsync();
        }

        public async Task AddCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }

}
