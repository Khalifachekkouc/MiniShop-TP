using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;
using MiniShop.Services;
using MiniShop.ViewModels;

namespace MiniShop.Areas.Admin.Controllers
{
    // Admin dashboard for the catalogue, served under /admin. To protect it later, add [Authorize] on this class.
    [Area("Admin")]
    public partial class ProduitsController : Controller
    {
        private const string FormFields = nameof(Produit.Name) + "," + nameof(Produit.Description) + "," +
                                          nameof(Produit.Prix) + "," + nameof(Produit.CategoryId) + "," + nameof(Produit.Stock) + "," +
                                          nameof(Produit.Rating) + "," + nameof(Produit.ReviewCount) + "," + nameof(Produit.SoldCount);

        private readonly ApplicationDbContext context;
        private readonly ImageStorage images;
        private readonly CategoryMenu menu;

        public ProduitsController(ApplicationDbContext context, ImageStorage images, CategoryMenu menu)
        {
            this.context = context;
            this.images = images;
            this.menu = menu;
        }

        [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
        private static partial Regex HexColor();

        // GET /admin  or  /admin?q=air&category=2 (a category id)
        public async Task<IActionResult> Index(string? q, int? category)
        {
            var all = await context.Produits.AsNoTracking().Include(p => p.Colors).Include(p => p.Storages).AsSplitQuery()
                .OrderByDescending(p => p.Prix)
                .ToListAsync();

            return View(DashboardViewModel.Build(all, await menu.AllAsync(), q, category));
        }

        public async Task<IActionResult> Details(int id)
        {
            var produit = await Load(id, tracked: false);
            return produit == null ? ProductNotFound() : View(produit);
        }

        // GET /admin/Create?category=2 preselects that category.
        [HttpGet]
        public async Task<IActionResult> Create(int? category)
        {
            var categories = await menu.AllAsync();
            var chosen = categories.FirstOrDefault(c => c.Id == category) ?? categories.FirstOrDefault();
            return await FormView(new Produit { CategoryId = chosen?.Id ?? 0, Stock = 1 }, [], []);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(FormFields)] Produit produit, IFormFile? imageFile,
                                                List<ColorInput>? colors, List<StorageInput>? storages)
        {
            colors ??= [];
            storages ??= [];
            ValidateImage(imageFile, "imageFile");
            ValidateVariants(colors, storages, existing: null);
            await ValidateCategory(produit.CategoryId);
            if (!ModelState.IsValid) return await FormView(produit, colors, storages);

            // Without a photo the product shows the neutral placeholder (ProductDisplay.ImagePath).
            produit.Image = imageFile != null ? await images.SaveAsync(imageFile, HttpContext.RequestAborted) : "";
            await ApplyVariants(produit, colors, storages, []);

            context.Produits.Add(produit);
            await context.SaveChangesAsync();

            TempData[Toast.Success] = $"« {produit.Name} » a été ajouté.";
            return await BackToCategory(produit.CategoryId);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var produit = await Load(id, tracked: false);
            if (produit == null) return ProductNotFound();
            var colorRows = produit.OrderedColors().Select(c => new ColorRow(c.Id.ToString(CultureInfo.InvariantCulture), c.Id, c.Name, c.Hex,
                c.Image == "" ? null : Url.Content("~/images/" + c.Image))).ToList();
            var storageRows = produit.OrderedStorages().Select(s => new StorageRow(s.Id.ToString(CultureInfo.InvariantCulture), s.Id, s.Label, s.Price)).ToList();
            return await FormView(produit, colorRows, storageRows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(FormFields)] Produit form, IFormFile? imageFile,
                                              List<ColorInput>? colors, List<StorageInput>? storages)
        {
            var produit = await Load(id, tracked: true);
            if (produit == null) return ProductNotFound();
            colors ??= [];
            storages ??= [];

            ValidateImage(imageFile, "imageFile");
            ValidateVariants(colors, storages, produit);
            await ValidateCategory(form.CategoryId);
            if (!ModelState.IsValid)
            {
                form.Id = id;
                form.Image = produit.Image; // keep showing the current picture
                form.Category = produit.Category; // the breadcrumb shows where the product is now
                return await FormView(form, colors, storages, produit);
            }

            var oldImage = produit.Image;

            produit.Name = form.Name;
            produit.Description = form.Description;
            produit.Prix = form.Prix;
            produit.CategoryId = form.CategoryId;
            produit.Stock = form.Stock;
            produit.Rating = form.Rating;
            produit.ReviewCount = form.ReviewCount;
            produit.SoldCount = form.SoldCount;

            if (imageFile != null)
                produit.Image = await images.SaveAsync(imageFile, HttpContext.RequestAborted);

            var orphans = new List<string>();
            await ApplyVariants(produit, colors, storages, orphans);

            await context.SaveChangesAsync();
            if (produit.Image != oldImage) images.DeleteIfUploaded(oldImage);
            foreach (var file in orphans) images.DeleteIfUploaded(file);

            TempData[Toast.Success] = $"« {produit.Name} » a été mis à jour.";
            return await BackToCategory(produit.CategoryId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var produit = await Load(id, tracked: true);
            if (produit == null)
            {
                TempData[Toast.Error] = "Ce produit n'existe plus : il a peut-être déjà été supprimé.";
                return RedirectToAction(nameof(Index));
            }

            var files = produit.Colors.Select(c => c.Image).Append(produit.Image).ToList();
            context.Produits.Remove(produit); // colours and storage tiers go with it (cascade)
            await context.SaveChangesAsync();
            foreach (var file in files) images.DeleteIfUploaded(file);

            TempData[Toast.Success] = $"« {produit.Name} » a été supprimé.";
            return await BackToCategory(produit.CategoryId);
        }

        private Task<Produit?> Load(int id, bool tracked)
        {
            var q = context.Produits.Include(p => p.Category).Include(p => p.Colors).Include(p => p.Storages).AsSplitQuery();
            return (tracked ? q : q.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id);
        }

        // ---------- Colours and storage tiers ----------

        private void ValidateVariants(List<ColorInput> colors, List<StorageInput> storages, Produit? existing)
        {
            if (colors.Count > ProductColor.MaxPerProduct)
                ModelState.AddModelError("", $"{ProductColor.MaxPerProduct} couleurs au maximum.");
            if (storages.Count > ProductStorage.MaxPerProduct)
                ModelState.AddModelError("", $"{ProductStorage.MaxPerProduct} capacités au maximum.");

            for (var i = 0; i < colors.Count; i++)
            {
                var c = colors[i];
                var n = i + 1;
                var name = c.Name?.Trim() ?? "";
                if (name.Length is < 1 or > 40) ModelState.AddModelError("", $"Couleur {n} : indiquez un nom de 40 caractères au plus.");
                if (c.Hex == null || !HexColor().IsMatch(c.Hex)) ModelState.AddModelError("", $"Couleur {n} : la teinte doit être au format #RRGGBB.");
                if (c.Id != null && existing?.Colors.Any(x => x.Id == c.Id) != true)
                    ModelState.AddModelError("", $"Couleur {n} : cette couleur n'appartient pas à ce produit.");
                if (c.ImageFile != null && images.Validate(c.ImageFile) is string error)
                    ModelState.AddModelError("", $"Couleur {n} : {error}");
            }

            for (var i = 0; i < storages.Count; i++)
            {
                var s = storages[i];
                var n = i + 1;
                var label = s.Label?.Trim() ?? "";
                if (label.Length is < 1 or > 20) ModelState.AddModelError("", $"Capacité {n} : indiquez un libellé de 20 caractères au plus.");
                if (s.Price is not (>= 0.01m and <= 9999999.99m))
                    ModelState.AddModelError("", $"Capacité {n} : le prix doit être compris entre 0,01 et 9 999 999,99 MAD.");
                if (s.Id != null && existing?.Storages.Any(x => x.Id == s.Id) != true)
                    ModelState.AddModelError("", $"Capacité {n} : cette capacité n'appartient pas à ce produit.");
            }
        }

        // Rows are already validated. Posted order = display order. orphans collects the files of
        // removed or replaced colour photos, deleted once the database is saved.
        private async Task ApplyVariants(Produit produit, List<ColorInput> colors, List<StorageInput> storages, List<string> orphans)
        {
            var keptColors = new List<ProductColor>();
            for (var i = 0; i < colors.Count; i++)
            {
                var input = colors[i];
                var color = input.Id is int id ? produit.Colors.First(x => x.Id == id) : new ProductColor();
                color.Name = input.Name!.Trim();
                color.Hex = input.Hex!.ToUpperInvariant();
                color.SortOrder = i;
                if (input.ImageFile != null)
                {
                    if (color.Image != "") orphans.Add(color.Image);
                    color.Image = await images.SaveAsync(input.ImageFile, HttpContext.RequestAborted);
                }
                keptColors.Add(color);
            }
            foreach (var removed in produit.Colors.Except(keptColors).ToList())
            {
                if (removed.Image != "") orphans.Add(removed.Image);
                produit.Colors.Remove(removed);
                if (removed.Id != 0) context.ProductColors.Remove(removed);
            }
            foreach (var c in keptColors.Where(c => c.Id == 0 && !produit.Colors.Contains(c))) produit.Colors.Add(c);

            var keptStorages = new List<ProductStorage>();
            for (var i = 0; i < storages.Count; i++)
            {
                var input = storages[i];
                var storage = input.Id is int id ? produit.Storages.First(x => x.Id == id) : new ProductStorage();
                storage.Label = input.Label!.Trim();
                storage.Price = input.Price!.Value;
                storage.SortOrder = i;
                keptStorages.Add(storage);
            }
            foreach (var removed in produit.Storages.Except(keptStorages).ToList())
            {
                produit.Storages.Remove(removed);
                if (removed.Id != 0) context.ProductStorages.Remove(removed);
            }
            foreach (var s in keptStorages.Where(s => s.Id == 0 && !produit.Storages.Contains(s))) produit.Storages.Add(s);

            // With storage tiers the product's price is the starting price: the cheapest tier.
            if (produit.Storages.Count > 0) produit.Prix = produit.Storages.Min(s => s.Price);
        }

        // ---------- Category ----------

        private async Task ValidateCategory(int categoryId)
        {
            if (categoryId > 0 && !(await menu.AllAsync()).Any(c => c.Id == categoryId))
                ModelState.AddModelError(nameof(Produit.CategoryId), "Cette catégorie n'existe plus : choisissez-en une autre.");
        }

        // Back to the dashboard, scrolled to the product's category.
        private async Task<IActionResult> BackToCategory(int categoryId)
        {
            var category = (await menu.AllAsync()).FirstOrDefault(c => c.Id == categoryId);
            return RedirectToAction(nameof(Index), null, category?.Slug);
        }

        // ---------- Form view ----------

        private async Task<ViewResult> FormView(Produit model, List<ColorRow> colorRows, List<StorageRow> storageRows)
        {
            ViewData["ColorRows"] = colorRows;
            ViewData["StorageRows"] = storageRows;
            ViewData["Categories"] = await menu.AllAsync();
            return View(model);
        }

        // Re-shows the posted rows after a validation error (a chosen file has to be picked again).
        private Task<ViewResult> FormView(Produit model, List<ColorInput> colors, List<StorageInput> storages, Produit? existing = null)
        {
            var colorRows = colors.Select((c, i) =>
            {
                var current = c.Id is int id ? existing?.Colors.FirstOrDefault(x => x.Id == id) : null;
                return new ColorRow($"r{i}", current?.Id, c.Name ?? "", c.Hex ?? "#000000",
                    current is { Image: not "" } ? Url.Content("~/images/" + current.Image) : null);
            }).ToList();
            var storageRows = storages.Select((s, i) =>
            {
                var current = s.Id is int id ? existing?.Storages.FirstOrDefault(x => x.Id == id) : null;
                return new StorageRow($"r{i}", current?.Id, s.Label ?? "", s.Price);
            }).ToList();
            return FormView(model, colorRows, storageRows);
        }

        private void ValidateImage(IFormFile? file, string key)
        {
            if (file == null) return;
            var error = images.Validate(file);
            if (error != null) ModelState.AddModelError(key, error);
        }

        private ViewResult ProductNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }
    }
}
