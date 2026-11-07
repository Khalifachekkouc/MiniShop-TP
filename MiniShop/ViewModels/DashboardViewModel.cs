using MiniShop.Models;

namespace MiniShop.ViewModels
{
    public static class Toast
    {
        public const string Success = "toast.success";
        public const string Error = "toast.error";
    }

    public record CategorySection(Category Category, IReadOnlyList<Produit> Products, int TotalInCategory);

    public class DashboardViewModel
    {
        public string? Query { get; init; }
        public Category? CategoryFilter { get; init; }
        public bool IsFiltered => !string.IsNullOrWhiteSpace(Query) || CategoryFilter != null;

        public IReadOnlyList<Category> Categories { get; init; } = [];

        // Stats always describe the whole catalogue, never the filtered view.
        public int TotalProducts { get; init; }
        public int TotalUnits { get; init; }
        public int LowStock { get; init; }
        public int OutOfStock { get; init; }
        public IReadOnlyDictionary<int, int> CountByCategory { get; init; } = new Dictionary<int, int>();

        public IReadOnlyList<CategorySection> Sections { get; init; } = [];
        public int MatchCount => Sections.Sum(s => s.Products.Count);

        // categories: in display order. An unknown category id is ignored (no filter).
        public static DashboardViewModel Build(IReadOnlyList<Produit> all, IReadOnlyList<Category> categories, string? q, int? categoryId)
        {
            var query = q?.Trim();
            if (string.IsNullOrEmpty(query)) query = null;
            var filter = categories.FirstOrDefault(c => c.Id == categoryId);

            IEnumerable<Produit> matches = all;
            if (filter != null) matches = matches.Where(p => p.CategoryId == filter.Id);
            if (query != null)
                matches = matches.Where(p =>
                    p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase));
            var matchList = matches.ToList();

            var sections = categories
                .Where(c => filter == null || c.Id == filter.Id)
                .Select(c => new CategorySection(
                    c,
                    matchList.Where(p => p.CategoryId == c.Id).ToList(),
                    all.Count(p => p.CategoryId == c.Id)))
                .ToList();

            return new DashboardViewModel
            {
                Query = query,
                CategoryFilter = filter,
                Categories = categories,
                TotalProducts = all.Count,
                TotalUnits = all.Sum(p => p.Stock),
                LowStock = all.Count(p => p.Stock <= Produit.LowStockThreshold),
                OutOfStock = all.Count(p => p.IsOutOfStock),
                CountByCategory = categories.ToDictionary(c => c.Id, c => all.Count(p => p.CategoryId == c.Id)),
                Sections = sections,
            };
        }
    }
}
