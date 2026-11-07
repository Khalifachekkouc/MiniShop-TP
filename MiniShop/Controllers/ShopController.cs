using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Services;
using MiniShop.ViewModels;

namespace MiniShop.Controllers
{
    // Public storefront: home and product pages.
    public class ShopController : Controller
    {
        private const int BestSellerCount = 6;
        private const int RelatedCount = 4;

        private readonly ApplicationDbContext context;
        private readonly CategoryMenu menu;

        public ShopController(ApplicationDbContext context, CategoryMenu menu)
        {
            this.context = context;
            this.menu = menu;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var all = await context.Produits.AsNoTracking().Include(p => p.Colors).Include(p => p.Storages).AsSplitQuery().ToListAsync();
            var bySales = all.OrderByDescending(p => p.SoldCount).ThenBy(p => p.Id).ToList();
            // The hero sells something you can buy and shows it well: the best seller that is in
            // stock and has a photo; failing that, the best seller in stock.
            var hero = bySales.FirstOrDefault(p => !p.IsOutOfStock && p.HasPhoto())
                       ?? bySales.FirstOrDefault(p => !p.IsOutOfStock)
                       ?? bySales.FirstOrDefault();

            return View(new HomeViewModel
            {
                Hero = hero,
                HeroIsTopSeller = hero != null && hero.Id == bySales[0].Id,
                BestSellers = bySales.Where(p => p.Id != hero?.Id).Take(BestSellerCount).ToList(),
                // Every category gets its section, in the admin's order, even an empty one.
                Sections = (await menu.AllAsync())
                    .Select(c => new ShopSection(c, all.Where(p => p.CategoryId == c.Id)
                        .OrderByDescending(p => p.SoldCount).ThenBy(p => p.Id).ToList()))
                    .ToList(),
                ProductCount = all.Count,
            });
        }

        [HttpGet("produit/{id:int}")]
        public async Task<IActionResult> Product(int id)
        {
            var product = await context.Produits.AsNoTracking().Include(p => p.Category)
                .Include(p => p.Colors).Include(p => p.Storages).AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("NotFound");
            }

            var related = await context.Produits.AsNoTracking().Include(p => p.Colors).Include(p => p.Storages).AsSplitQuery()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .OrderByDescending(p => p.SoldCount).ThenBy(p => p.Id)
                .Take(RelatedCount)
                .ToListAsync();

            return View(new ProductPageViewModel { Product = product, Related = related });
        }
    }
}
