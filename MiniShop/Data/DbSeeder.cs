using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data
{
    // Demo catalogue. Runs from UseSeeding, i.e. on `dotnet ef database update` and on Database.Migrate().
    // It only inserts when none of these products exist yet, so it never duplicates rows
    // and never brings back products the admin deleted (unless every one of them is gone).
    // The three categories come from the AddCategories migration; a missing one is created again.
    //
    // The catalogue holds only products the client has photographed: their photos and colours are in
    // CatalogImages.g.cs (ClientImages, ClientColors), keyed by product slug.
    // Storage tiers: Apple's published tiers per model.
    public static partial class DbSeeder
    {
        private record Tier(string Label, decimal Price);

        private record Item(string Category, Produit Product);

        // Name, slug and order of the seeded categories (the same as in the AddCategories migration).
        private static readonly (string Name, string Slug)[] SeedCategories =
            [("iPhone", "iphone"), ("Mac", "mac"), ("Accessoires", "accessoires")];

        private static Item P(string category, string slug, string name, int stock, decimal rating, int reviews, int sold,
                              string description, params Tier[] tiers)
        {
            var colors = ClientColors.GetValueOrDefault(slug) ?? [];
            var image = colors.Length > 0 ? colors[0].Image : ClientImages.GetValueOrDefault(slug) ?? "";
            return new Item(category, new Produit
            {
                Image = image,
                Name = name,
                // With storage tiers the product's price is the starting price: the cheapest tier.
                Prix = tiers.Min(t => t.Price),
                Stock = stock,
                Rating = rating,
                ReviewCount = reviews,
                SoldCount = sold,
                Description = description,
                Colors = colors.Select((col, i) => new ProductColor { Name = col.Name, Hex = col.Hex, Image = col.Image, SortOrder = i }).ToList(),
                // A single price is a product without storage options.
                Storages = tiers.Length > 1 || tiers[0].Label != ""
                    ? tiers.Select((t, i) => new ProductStorage { Label = t.Label, Price = t.Price, SortOrder = i }).ToList()
                    : [],
            });
        }

        private static Tier T(string label, decimal price) => new(label, price);
        private static Tier Only(decimal price) => new("", price);

        private static IReadOnlyList<Item> Products() =>
        [
            // iPhone: release status checked on 2026-11-07. Only verified specs are written down.
            P("iphone", "iphone-18-pro-max", "iPhone 18 Pro Max", 6, 4.8m, 214, 520,
                "Le plus récent des iPhone Pro, sorti en septembre 2026. La puce A20 Pro et une Dynamic Island plus compacte.",
                T("256 Go", 17990m), T("512 Go", 20490m), T("1 To", 24990m), T("2 To", 30990m)),
            P("iphone", "iphone-17-pro-max", "iPhone 17 Pro Max", 12, 4.9m, 1048, 1840,
                "Un écran 6,9 pouces Super Retina XDR avec ProMotion. La puce A19 Pro et le boîtier en aluminium gardent l'appareil frais en charge. Triple caméra Fusion 48 Mpx.",
                T("256 Go", 15990m), T("512 Go", 18490m), T("1 To", 22990m), T("2 To", 28990m)),
            P("iphone", "iphone-16-pro-max", "iPhone 16 Pro Max", 0, 4.7m, 1180, 1390,
                "Écran 6,9 pouces, puce A18 Pro et boîtier en titane. Bouton Commande de l'appareil photo et téléobjectif 5x.",
                T("256 Go", 13490m), T("512 Go", 15990m), T("1 To", 19990m)),
            P("iphone", "iphone-air", "iPhone Air", 4, 4.4m, 287, 640,
                "Le plus fin des iPhone, avec un seul module photo à l'arrière et la puce A19 Pro.",
                T("256 Go", 13990m), T("512 Go", 16490m), T("1 To", 20490m)),
            P("iphone", "iphone-17", "iPhone 17", 20, 4.7m, 834, 1510,
                "L'écran 6,3 pouces passe à 120 Hz et reste allumé en permanence. Puce A19, double caméra Fusion 48 Mpx et caméra avant Center Stage.",
                T("256 Go", 10990m), T("512 Go", 13490m)),
            P("iphone", "iphone-16", "iPhone 16", 9, 4.6m, 1290, 1460,
                "Écran 6,1 pouces, puce A18 et bouton Commande de l'appareil photo. Un choix sûr à prix plus doux.",
                T("128 Go", 8490m), T("256 Go", 9490m), T("512 Go", 11990m)),

            // Mac: the client's laptops, newest first.
            P("mac", "macbook-air-13-m5", "MacBook Air 13\" M5", 10, 4.8m, 742, 1190,
                "Le portable le plus léger de la gamme, sans ventilateur et silencieux. Écran Liquid Retina de 13,6 pouces et puce M5.",
                T("512 Go", 12990m), T("1 To", 15490m), T("2 To", 20490m), T("4 To", 30490m)),
            P("mac", "macbook-pro-14-m5", "MacBook Pro 14\" M5", 4, 4.8m, 120, 210,
                "Le MacBook Pro 14 pouces avec la puce M5, sorti en octobre 2025. Coloris noir sidéral.",
                T("1 To", 18490m), T("2 To", 23490m), T("4 To", 33490m)),
            P("mac", "macbook-pro-14-m4", "MacBook Pro 14\" M4", 3, 4.8m, 310, 520,
                "La génération précédente du MacBook Pro 14 pouces, avec la puce M4. Coloris noir sidéral.",
                T("512 Go", 16990m), T("1 To", 19490m), T("2 To", 24490m)),
            P("mac", "macbook-air-13-m4", "MacBook Air 13\" M4", 7, 4.8m, 520, 980,
                "La génération précédente du MacBook Air 13 pouces, sans ventilateur, avec la puce M4.",
                T("256 Go", 11490m), T("512 Go", 13490m), T("1 To", 15990m), T("2 To", 20990m)),
            P("mac", "macbook-neo-13", "MacBook Neo 13\" A18 Pro", 12, 4.4m, 210, 450,
                "Le MacBook le plus abordable de la gamme, avec la puce A18 Pro de l'iPhone.",
                T("256 Go", 6490m), T("512 Go", 7490m)),


            P("accessoires", "airpods-pro-3", "AirPods Pro 3", 25, 4.7m, 1530, 1780,
                "Une réduction de bruit deux fois plus efficace et un capteur de fréquence cardiaque. La traduction en direct fonctionne avec l'iPhone. Jusqu'à 8 heures d'écoute.",
                Only(2990m)),
            P("accessoires", "airpods-4-anc", "AirPods 4 avec réduction active du bruit", 18, 4.5m, 960, 1350,
                "Un design ouvert, sans embout, avec réduction active du bruit. Audio spatial personnalisé et boîtier USB-C.",
                Only(2090m)),
            P("accessoires", "magic-keyboard-touch-id", "Magic Keyboard avec Touch ID", 7, 4.3m, 214, 380,
                "Un clavier sans fil précis avec Touch ID pour déverrouiller le Mac et payer. Se recharge en USB-C. Disposition AZERTY.",
                Only(1790m)),
            P("accessoires", "airpods-max-2", "AirPods Max 2", 5, 4.5m, 64, 150,
                "Le casque circum-auriculaire d'Apple, avec réduction active du bruit et audio spatial.",
                Only(6490m)),
            P("accessoires", "apple-watch-series-11", "Apple Watch Series 11", 8, 4.7m, 240, 510,
                "L'Apple Watch principale de la gamme, pour la santé, le sport et les notifications. Bracelet Sport.",
                Only(4990m)),
            P("accessoires", "apple-watch-se-3", "Apple Watch SE 3", 10, 4.5m, 180, 420,
                "L'Apple Watch la plus abordable, pour suivre l'activité et recevoir les notifications. Bracelet Sport.",
                Only(2990m)),
            P("accessoires", "chargeur-20w", "Chargeur secteur USB-C 20 W", 50, 4.6m, 820, 1600,
                "Le chargeur compact pour recharger rapidement un iPhone. Fiche européenne.",
                Only(249m)),
            P("accessoires", "adaptateur-70w", "Adaptateur secteur USB-C 70 W", 15, 4.6m, 140, 300,
                "Un port USB-C pour recharger un MacBook Pro. Fiche européenne.",
                Only(749m)),
            P("accessoires", "cable-charge-magnetique-watch", "Câble de charge magnétique USB-C pour Apple Watch", 30, 4.4m, 190, 380,
                "Le chargeur magnétique de l'Apple Watch, avec un connecteur USB-C. Il se fixe seul au dos de la montre.",
                Only(349m)),
        ];

        public static void Seed(DbContext context)
        {
            var items = Products();
            var names = items.Select(i => i.Product.Name).ToList();
            if (context.Set<Produit>().Any(p => names.Contains(p.Name))) return;

            Attach(context, items, context.Set<Category>().ToList());
            context.SaveChanges();
        }

        public static async Task SeedAsync(DbContext context, CancellationToken ct)
        {
            var items = Products();
            var names = items.Select(i => i.Product.Name).ToList();
            if (await context.Set<Produit>().AnyAsync(p => names.Contains(p.Name), ct)) return;

            Attach(context, items, await context.Set<Category>().ToListAsync(ct));
            await context.SaveChangesAsync(ct);
        }

        // Puts each product in its category, creating again a seeded category the admin deleted.
        private static void Attach(DbContext context, IReadOnlyList<Item> items, List<Category> categories)
        {
            var bySlug = categories.ToDictionary(c => c.Slug);
            foreach (var (name, slug) in SeedCategories)
            {
                if (bySlug.ContainsKey(slug)) continue;
                var created = new Category
                {
                    Name = name,
                    Slug = slug,
                    SortOrder = categories.Count == 0 ? 1 : categories.Max(c => c.SortOrder) + 1,
                };
                categories.Add(created);
                bySlug[slug] = created;
                context.Set<Category>().Add(created);
            }
            foreach (var item in items)
            {
                item.Product.Category = bySlug[item.Category];
                context.Set<Produit>().Add(item.Product);
            }
        }
    }
}
