using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.ViewModels
{
    public static class ProductDisplay
    {
        public const string FallbackImage = "products/placeholder.webp";

        // The photo shown for the product (cards, hero, cart): the default colour's photo when the
        // product has colours (and they were loaded), else its own photo, else the neutral placeholder.
        // Path under wwwroot/images.
        public static string ImagePath(this Produit p) =>
            p.DefaultColor() is { Image: not "" } c ? c.Image : p.MainImagePath();

        // The product's own photo only (the admin form's main image).
        public static string MainImagePath(this Produit p) =>
            string.IsNullOrWhiteSpace(p.Image) ? FallbackImage : p.Image;

        public static ProductColor? DefaultColor(this Produit p) => p.Colors.OrderBy(c => c.SortOrder).FirstOrDefault();

        public static IEnumerable<ProductColor> OrderedColors(this Produit p) => p.Colors.OrderBy(c => c.SortOrder).ThenBy(c => c.Id);

        public static IEnumerable<ProductStorage> OrderedStorages(this Produit p) => p.Storages.OrderBy(s => s.SortOrder).ThenBy(s => s.Id);

        // A colour without its own photo shows the product's photo.
        public static string ColorImagePath(this Produit p, ProductColor? c) =>
            c is { Image: not "" } ? c.Image : p.MainImagePath();

        public const string HeroFolder = "products/hero/";

        public static bool HasPhoto(this Produit p) => p.ImagePath() != FallbackImage;

        // The home hero shows the product much larger than a card, so catalogue photos have a
        // variant at the source's full resolution in products/hero/.
        // Admin uploads have none and use their own file.
        public static string HeroImagePath(this Produit p)
        {
            var image = p.ImagePath();
            return p.HasPhoto() && image.StartsWith("products/") && !image.StartsWith(HeroFolder)
                ? HeroFolder + image["products/".Length..]
                : image;
        }

        // « À partir de » on cards when the product sells several storage tiers (Prix is the cheapest).
        public static bool HasStartingPrice(this Produit p) => p.Storages.Count > 1;

        public static string Price(this Produit p) => MoneyFormat.Mad(p.Prix);

        // A short preview of the description, cut at a word boundary (used in dense lists).
        public static string Excerpt(this Produit p, int max = 80)
        {
            var text = p.Description.Trim();
            if (text.Length <= max) return text;
            var cut = text.LastIndexOf(' ', max);
            return text[..(cut > max / 2 ? cut : max)].TrimEnd(',', ';', ':', '.') + "…";
        }

        public static string StockState(this Produit p) =>
            p.IsOutOfStock ? "out" : p.IsLowStock ? "low" : "ok";

        public static string StockLabel(this Produit p) =>
            p.IsOutOfStock ? "Rupture"
            : p.IsLowStock ? $"Stock faible : {MoneyFormat.Count(p.Stock)}"
            : $"{MoneyFormat.Count(p.Stock)} en stock";
    }
}
