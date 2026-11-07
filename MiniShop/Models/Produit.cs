using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniShop.Models
{
    public class Produit
    {
        public const int NameMaxLength = 120;
        public const int DescriptionMaxLength = 1000;
        public const int LowStockThreshold = 5;

        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(NameMaxLength, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre {2} et {1} caractères.")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "La description est obligatoire.")]
        [StringLength(DescriptionMaxLength, ErrorMessage = "La description ne doit pas dépasser {1} caractères.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Le prix est obligatoire.")]
        [Range(typeof(decimal), "0.01", "9999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
            ErrorMessage = "Le prix doit être compris entre 0,01 et 9 999 999,99 MAD.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Prix (MAD)")]
        public decimal Prix { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Choisissez une catégorie.")]
        [Display(Name = "Catégorie")]
        public int CategoryId { get; set; }

        [ValidateNever]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Le stock est obligatoire.")]
        [Range(0, 100000, ErrorMessage = "Le stock doit être compris entre {1} et {2}.")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "La note est obligatoire.")]
        [Range(typeof(decimal), "0", "5", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
            ErrorMessage = "La note doit être comprise entre 0 et 5.")]
        [Column(TypeName = "decimal(2,1)")]
        [Display(Name = "Note (0 à 5)")]
        public decimal Rating { get; set; }

        [Required(ErrorMessage = "Le nombre d'avis est obligatoire.")]
        [Range(0, 1000000, ErrorMessage = "Le nombre d'avis doit être compris entre {1} et {2}.")]
        [Display(Name = "Nombre d'avis")]
        public int ReviewCount { get; set; }

        [Required(ErrorMessage = "Le nombre de ventes est obligatoire.")]
        [Range(0, 10000000, ErrorMessage = "Le nombre de ventes doit être compris entre {1} et {2}.")]
        [Display(Name = "Exemplaires vendus")]
        public int SoldCount { get; set; }

        // File name inside wwwroot/images, set by the upload handler, never by the form.
        [StringLength(260)]
        [ValidateNever]
        public string Image { get; set; } = "";

        // Colour and storage options (stock stays per product). With storage tiers, Prix is the
        // starting price: the cheapest tier.
        [ValidateNever]
        public List<ProductColor> Colors { get; set; } = [];

        [ValidateNever]
        public List<ProductStorage> Storages { get; set; } = [];

        [NotMapped]
        public bool IsLowStock => Stock > 0 && Stock <= LowStockThreshold;

        [NotMapped]
        public bool IsOutOfStock => Stock <= 0;
    }
}
