using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;
using MiniShop.ViewModels;

namespace MiniShop.Areas.Admin.Controllers
{
    // The shop's aisles, served under /admin/categories. Each category is a section of the home page,
    // a tab, a footer link and a choice in the product form, in the order set here.
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext context;

        public CategoriesController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var rows = await context.Categories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .Select(c => new CategoryRow(c.Id, c.Name, c.Slug, c.Products.Count))
                .ToListAsync();
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Create(bool pourProduit = false)
        {
            return await FormView(null, new CategoryInput { PourProduit = pourProduit });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(nameof(CategoryInput.Name) + "," + nameof(CategoryInput.PourProduit))] CategoryInput input)
        {
            var all = await context.Categories.ToListAsync();
            var name = input.CleanName();
            ValidateName(name, all, self: null);
            if (!ModelState.IsValid) return await FormView(null, input);

            var category = new Category
            {
                Name = name,
                Slug = CategorySlug.Unique(name, all.Select(c => c.Slug)),
                SortOrder = all.Count == 0 ? 1 : all.Max(c => c.SortOrder) + 1,
            };
            context.Categories.Add(category);
            if (!await TrySave()) return await FormView(null, input);

            TempData[Toast.Success] = $"La catégorie « {category.Name} » a été créée.";
            return input.PourProduit
                ? RedirectToAction("Create", "Produits", new { category = category.Id })
                : RedirectToAction(nameof(Index), null, RowAnchor(category.Id));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return CategoryNotFound();
            return await FormView(category, new CategoryInput { Name = category.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(nameof(CategoryInput.Name))] CategoryInput input)
        {
            var all = await context.Categories.ToListAsync();
            var category = all.FirstOrDefault(c => c.Id == id);
            if (category == null) return CategoryNotFound();

            var name = input.CleanName();
            ValidateName(name, all, category);
            if (!ModelState.IsValid) return await FormView(category, input);

            // The address follows the name, so a renamed aisle keeps a readable anchor.
            if (name != category.Name)
            {
                category.Name = name;
                category.Slug = CategorySlug.Unique(name, all.Where(c => c.Id != id).Select(c => c.Slug));
            }
            if (!await TrySave()) return await FormView(category, input);

            TempData[Toast.Success] = $"La catégorie « {category.Name} » a été mise à jour.";
            return RedirectToAction(nameof(Index), null, RowAnchor(category.Id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                TempData[Toast.Error] = "Cette catégorie n'existe plus : elle a peut-être déjà été supprimée.";
                return RedirectToAction(nameof(Index));
            }

            var count = category.Products.Count;
            if (count > 0)
            {
                TempData[Toast.Error] = count == 1
                    ? $"« {category.Name} » contient un produit. Déplacez ou supprimez d'abord son produit."
                    : $"« {category.Name} » contient {count} produits. Déplacez ou supprimez d'abord ses {count} produits.";
                return RedirectToAction(nameof(Index), null, RowAnchor(id));
            }

            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            TempData[Toast.Success] = $"La catégorie « {category.Name} » a été supprimée.";
            return RedirectToAction(nameof(Index));
        }

        // Moves a category one place up (direction -1) or down (+1) in the shop's order.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, int direction)
        {
            var ordered = await context.Categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync();
            var i = ordered.FindIndex(c => c.Id == id);
            if (i < 0)
            {
                TempData[Toast.Error] = "Cette catégorie n'existe plus : elle a peut-être déjà été supprimée.";
                return RedirectToAction(nameof(Index));
            }

            var j = i + Math.Sign(direction);
            if (direction != 0 && j >= 0 && j < ordered.Count)
            {
                (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
                for (var k = 0; k < ordered.Count; k++) ordered[k].SortOrder = k + 1;
                await context.SaveChangesAsync();
                TempData[Toast.Success] = $"« {ordered[j].Name} » est maintenant en position {j + 1}.";
            }
            return RedirectToAction(nameof(Index), null, RowAnchor(id));
        }

        // ---------- Helpers ----------

        private void ValidateName(string name, IEnumerable<Category> all, Category? self)
        {
            const string key = nameof(CategoryInput.Name);
            var length = CategoryInput.VisibleLength(name);
            if (length == 0)
                ModelState.AddModelError(key, "Le nom est obligatoire.");
            else if (length < Category.NameMin || length > Category.NameMax || name.Length > Category.NameStorage)
                ModelState.AddModelError(key, $"Le nom doit contenir entre {Category.NameMin} et {Category.NameMax} caractères.");
            else if (all.FirstOrDefault(c => c != self && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) is Category twin)
                ModelState.AddModelError(key, $"La catégorie « {twin.Name} » existe déjà.");
        }

        // The unique indexes catch a duplicate created at the same moment by another tab.
        private async Task<bool> TrySave()
        {
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(CategoryInput.Name), "Une catégorie porte déjà ce nom.");
                return false;
            }
        }

        private async Task<ViewResult> FormView(Category? category, CategoryInput input)
        {
            var others = await context.Categories.AsNoTracking()
                .Where(c => category == null || c.Id != category.Id)
                .Select(c => c.Slug).ToListAsync();
            var count = category == null ? 0 : await context.Produits.CountAsync(p => p.CategoryId == category.Id);
            return View("Form", new CategoryFormViewModel
            {
                Id = category?.Id,
                Input = input,
                CurrentName = category == null ? null : context.Entry(category).Property(c => c.Name).OriginalValue,
                CurrentSlug = category == null ? null : context.Entry(category).Property(c => c.Slug).OriginalValue,
                TakenSlugs = others,
                ProductCount = count,
            });
        }

        private static string RowAnchor(int id) => $"categorie-{id}";

        private ViewResult CategoryNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }
    }
}
