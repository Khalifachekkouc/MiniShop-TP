using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Services
{
    // A cart line is one product in one colour and one storage tier.
    public record CartLine(Produit Product, ProductColor? Color, ProductStorage? Storage, int Quantity, int MaxQuantity)
    {
        public string Key => CartService.KeyOf(Product.Id, Color?.Id, Storage?.Id);
        public decimal UnitPrice => Storage?.Price ?? Product.Prix;
        public decimal LineTotal => UnitPrice * Quantity;

        // « Bleu intense, 512 Go »
        public string Options => string.Join(", ", new[] { Color?.Name, Storage?.Label }.Where(s => !string.IsNullOrEmpty(s)));
    }

    public class CartView
    {
        public IReadOnlyList<CartLine> Lines { get; init; } = [];
        public IReadOnlyList<string> Notices { get; init; } = [];
        public int ItemCount => Lines.Sum(l => l.Quantity);
        public decimal Total => Lines.Sum(l => l.LineTotal);
        public bool IsEmpty => Lines.Count == 0;
    }

    public enum AddResult { Added, Clamped, OutOfStock, NotFound, InvalidOption }

    public record AddOutcome(AddResult Result, int Quantity, Produit? Product, string Options);

    // Session cart. The session only holds ids and quantities: names, prices, options and stock are
    // always read from the database, so nothing the browser sends can change a price.
    public class CartService
    {
        public const int MaxPerLine = 10;
        private const string Key = "cart.v2";

        private record Entry(int P, int? C, int? S, int Q);

        private readonly IHttpContextAccessor http;
        private readonly ApplicationDbContext db;

        public CartService(IHttpContextAccessor http, ApplicationDbContext db)
        {
            this.http = http;
            this.db = db;
        }

        private ISession Session => http.HttpContext!.Session;

        public static int MaxFor(Produit p) => Math.Max(0, Math.Min(p.Stock, MaxPerLine));

        public static string KeyOf(int productId, int? colorId, int? storageId) => $"{productId}-{colorId ?? 0}-{storageId ?? 0}";

        // Number of items, read from the session only (used by the header badge on every page).
        public int Count() => Read().Sum(e => e.Q);

        private Task<Produit?> LoadProduct(int id) =>
            db.Produits.AsNoTracking().Include(p => p.Colors).Include(p => p.Storages).FirstOrDefaultAsync(p => p.Id == id);

        // colorId / storageId: null picks the product's default (first colour, cheapest tier).
        // An id that does not belong to the product is refused (InvalidOption), never trusted.
        public async Task<AddOutcome> AddAsync(int productId, int? colorId, int? storageId, int quantity)
        {
            var product = await LoadProduct(productId);
            if (product == null) return new(AddResult.NotFound, 0, null, "");

            var color = colorId is int c ? product.Colors.FirstOrDefault(x => x.Id == c) : product.Colors.OrderBy(x => x.SortOrder).FirstOrDefault();
            var storage = storageId is int s ? product.Storages.FirstOrDefault(x => x.Id == s) : product.Storages.OrderBy(x => x.SortOrder).FirstOrDefault();
            if ((colorId != null && color == null) || (storageId != null && storage == null))
                return new(AddResult.InvalidOption, 0, product, "");

            var options = new CartLine(product, color, storage, 0, 0).Options;
            var max = MaxFor(product);
            if (max == 0) return new(AddResult.OutOfStock, 0, product, options);

            var lines = Read();
            var i = lines.FindIndex(e => e.P == productId && e.C == color?.Id && e.S == storage?.Id);
            var current = i >= 0 ? lines[i].Q : 0;
            var wanted = current + Math.Max(1, quantity);
            var next = Math.Min(wanted, max);
            var entry = new Entry(productId, color?.Id, storage?.Id, next);
            if (i >= 0) lines[i] = entry; else lines.Add(entry);
            Write(lines);
            return new(next < wanted ? AddResult.Clamped : AddResult.Added, next, product, options);
        }

        public async Task<int> SetAsync(string lineKey, int quantity)
        {
            var lines = Read();
            var i = lines.FindIndex(e => KeyOf(e.P, e.C, e.S) == lineKey);
            if (i < 0) return 0;

            var product = await db.Produits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == lines[i].P);
            var max = product == null ? 0 : MaxFor(product);
            if (max == 0) { lines.RemoveAt(i); Write(lines); return 0; }

            lines[i] = lines[i] with { Q = Math.Clamp(quantity, 1, max) };
            Write(lines);
            return lines[i].Q;
        }

        public void Remove(string lineKey)
        {
            var lines = Read();
            if (lines.RemoveAll(e => KeyOf(e.P, e.C, e.S) == lineKey) > 0) Write(lines);
        }

        public void Clear() => Session.Remove(Key);

        // Loads the cart against the database. Lines whose product or option disappeared, or that ran
        // out, are dropped, and quantities above the current stock are lowered, with a notice each time.
        public async Task<CartView> LoadAsync()
        {
            var lines = Read();
            if (lines.Count == 0) return new CartView();

            var ids = lines.Select(e => e.P).Distinct().ToList();
            var products = await db.Produits.AsNoTracking().Include(p => p.Colors).Include(p => p.Storages)
                .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var result = new List<CartLine>();
            var notices = new List<string>();
            var keep = new List<Entry>();

            foreach (var e in lines)
            {
                if (!products.TryGetValue(e.P, out var product))
                {
                    notices.Add("Un article n'est plus disponible et a été retiré du panier.");
                    continue;
                }
                var color = e.C is int c ? product.Colors.FirstOrDefault(x => x.Id == c) : null;
                var storage = e.S is int s ? product.Storages.FirstOrDefault(x => x.Id == s) : null;
                if ((e.C != null && color == null) || (e.S != null && storage == null))
                {
                    notices.Add($"Une option de « {product.Name} » n'existe plus : l'article a été retiré du panier.");
                    continue;
                }

                var max = MaxFor(product);
                if (max == 0)
                {
                    notices.Add($"« {product.Name} » est épuisé et a été retiré du panier.");
                    continue;
                }

                var q = Math.Clamp(e.Q, 1, max);
                if (q != e.Q)
                    notices.Add($"La quantité de « {product.Name} » a été ramenée à {q}, selon le stock disponible.");
                keep.Add(e with { Q = q });
                result.Add(new CartLine(product, color, storage, q, max));
            }

            if (keep.Count != lines.Count || keep.Zip(lines).Any(x => x.First != x.Second)) Write(keep);
            return new CartView { Lines = result, Notices = notices };
        }

        private List<Entry> Read()
        {
            var json = Session.GetString(Key);
            if (string.IsNullOrEmpty(json)) return [];
            try
            {
                var data = JsonSerializer.Deserialize<List<Entry>>(json) ?? [];
                return data.Where(e => e.Q > 0).Select(e => e with { Q = Math.Min(e.Q, MaxPerLine) }).ToList();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private void Write(List<Entry> lines) => Session.SetString(Key, JsonSerializer.Serialize(lines));
    }
}
