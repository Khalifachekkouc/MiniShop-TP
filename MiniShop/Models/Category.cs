using System.Globalization;
using System.Text;

namespace MiniShop.Models
{
    // A shop aisle (rayon), managed in the admin. Its slug is the anchor of its section on the home
    // page (/#iphone) and of its block on the dashboard; it follows the name.
    public class Category
    {
        public const int NameMin = 2;
        public const int NameMax = 30;      // in visible characters: an emoji counts as one
        public const int NameStorage = 60;  // column size, room for characters stored as two units

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public int SortOrder { get; set; }

        public List<Produit> Products { get; set; } = [];
    }

    public static class CategorySlug
    {
        public const int MaxLength = 40;

        // Ids already used on the pages where a slug becomes an element id.
        private static readonly HashSet<string> Reserved =
            ["main", "meilleures-ventes", "hero-title", "hero-product-name", "q", "category"];

        // "Accessoires & Câbles" -> "accessoires-cables". Never empty, never a reserved id.
        public static string From(string name)
        {
            var sb = new StringBuilder();
            foreach (var ch in name.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                var c = char.ToLowerInvariant(ch) switch { 'œ' => "oe", 'æ' => "ae", 'ß' => "ss", var o => o.ToString() };
                foreach (var k in c)
                    sb.Append(k is >= 'a' and <= 'z' or >= '0' and <= '9' ? k : '-');
            }
            var slug = string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
            if (slug.Length > MaxLength) slug = slug[..MaxLength].TrimEnd('-');
            return slug == "" ? "rayon" : slug;
        }

        // From(name), with -2, -3 ... when another category or a page element already has it.
        public static string Unique(string name, IEnumerable<string> taken)
        {
            var used = new HashSet<string>(taken, StringComparer.Ordinal);
            used.UnionWith(Reserved);
            var root = From(name);
            var slug = root;
            for (var n = 2; used.Contains(slug); n++) slug = $"{root}-{n}";
            return slug;
        }
    }
}
