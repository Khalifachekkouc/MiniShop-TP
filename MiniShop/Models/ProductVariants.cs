using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniShop.Models
{
    // One colour of a product, with its own photo. The first one (lowest SortOrder) is the default:
    // its photo is the one used on cards, in the hero and in the cart.
    public class ProductColor
    {
        public const int MaxPerProduct = 8;

        public int Id { get; set; }
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        [Required, StringLength(40)]
        public string Name { get; set; } = "";

        // Swatch colour, #RRGGBB.
        [Required, StringLength(7), RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string Hex { get; set; } = "#000000";

        // Path under wwwroot/images; empty means "use the product's own photo".
        [StringLength(260)]
        public string Image { get; set; } = "";

        public int SortOrder { get; set; }
    }

    // One storage tier. Price is the full price of the product in that tier (not a surcharge).
    public class ProductStorage
    {
        public const int MaxPerProduct = 6;

        public int Id { get; set; }
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        [Required, StringLength(20)]
        public string Label { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int SortOrder { get; set; }
    }
}
