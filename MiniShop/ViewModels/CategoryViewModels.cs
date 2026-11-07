using System.Globalization;
using System.Text.RegularExpressions;

namespace MiniShop.ViewModels
{
    // One line of the admin's category list.
    public record CategoryRow(int Id, string Name, string Slug, int ProductCount);

    // The category form. PourProduit: opened from the product form, so saving goes back to it.
    public partial class CategoryInput
    {
        public string? Name { get; set; }
        public bool PourProduit { get; set; }

        // Trimmed, inner runs of spaces folded into one, control characters dropped.
        public string CleanName() =>
            Spaces().Replace(new string((Name ?? "").Where(c => !char.IsControl(c)).ToArray()), " ").Trim();

        // Visible characters: an emoji or a letter with its accent counts as one.
        public static int VisibleLength(string s) => new StringInfo(s).LengthInTextElements;

        [GeneratedRegex(@"\s+")]
        private static partial Regex Spaces();
    }

    public class CategoryFormViewModel
    {
        public int? Id { get; init; }
        public required CategoryInput Input { get; init; }
        public string? CurrentName { get; init; }
        public string? CurrentSlug { get; init; }
        // Slugs of the other categories, for the live address preview.
        public IReadOnlyList<string> TakenSlugs { get; init; } = [];
        public int ProductCount { get; init; }
    }

    // Texts of the shared delete dialog (Areas/Admin/Views/Shared/_DeleteModal.cshtml).
    public record DeleteDialog(string Controller, string Title, string Subject, string Consequence)
    {
        public static readonly DeleteDialog Product =
            new("Produits", "Supprimer ce produit ?", "Ce produit", "sera retiré du catalogue. Cette action est définitive.");

        public static readonly DeleteDialog Category =
            new("Categories", "Supprimer cette catégorie ?", "Cette catégorie", "sera supprimée et son rayon disparaîtra de la boutique. Cette action est définitive.");
    }
}
