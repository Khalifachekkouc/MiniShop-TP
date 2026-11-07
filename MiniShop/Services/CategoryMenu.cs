using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Services
{
    // The shop's aisles in display order, read once per request (header, footer, pages).
    public class CategoryMenu
    {
        private readonly ApplicationDbContext context;
        private IReadOnlyList<Category>? categories;

        public CategoryMenu(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IReadOnlyList<Category>> AllAsync() =>
            categories ??= await context.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync();
    }
}
