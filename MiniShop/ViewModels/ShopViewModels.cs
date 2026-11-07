using System.ComponentModel.DataAnnotations;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.ViewModels
{
    public record ShopSection(Category Category, IReadOnlyList<Produit> Products);

    public class HomeViewModel
    {
        public const int HeroAisleLinks = 5;

        public Produit? Hero { get; init; }
        public bool HeroIsTopSeller { get; init; }
        public IReadOnlyList<Produit> BestSellers { get; init; } = [];
        public IReadOnlyList<ShopSection> Sections { get; init; } = [];
        public int ProductCount { get; init; }
    }

    public class ProductPageViewModel
    {
        public required Produit Product { get; init; }
        public IReadOnlyList<Produit> Related { get; init; } = [];
        public int MaxQuantity => CartService.MaxFor(Product);
    }

    public enum PaymentMethod
    {
        [Display(Name = "Carte")] Card = 0,
        [Display(Name = "Paiement à la livraison")] CashOnDelivery = 1
    }

    // Only contact and delivery details reach the server. The card fields on the
    // payment page have no name attribute, so the browser never sends them.
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Indiquez votre nom complet.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre {2} et {1} caractères.")]
        [Display(Name = "Nom complet")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Indiquez votre adresse e-mail.")]
        [EmailAddress(ErrorMessage = "Cette adresse e-mail n'est pas valide.")]
        [StringLength(120)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Indiquez votre numéro de téléphone.")]
        [RegularExpression(@"^\s*(\+212|00212|0)\s*[5-7](\s*\d){8}\s*$",
            ErrorMessage = "Numéro marocain attendu, par exemple 06 12 34 56 78.")]
        [Display(Name = "Téléphone")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Indiquez la ville de livraison.")]
        [StringLength(60, ErrorMessage = "La ville ne doit pas dépasser {1} caractères.")]
        [Display(Name = "Ville")]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "Indiquez l'adresse de livraison.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "L'adresse doit contenir entre {2} et {1} caractères.")]
        [Display(Name = "Adresse")]
        public string Address { get; set; } = "";

        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Mode de paiement inconnu.")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    }

    public class CheckoutViewModel
    {
        public required CheckoutForm Form { get; init; }
        public required CartView Cart { get; init; }
    }

    public record OrderLine(string Name, string Options, int Quantity, decimal Total);

    public record OrderConfirmation(string Number, string FullName, string Email, string City, PaymentMethod Method, int ItemCount, decimal Total,
                                    IReadOnlyList<OrderLine> Lines);

    public static class Cities
    {
        public static readonly string[] Morocco =
        [
            "Casablanca", "Rabat", "Marrakech", "Fès", "Tanger", "Agadir", "Meknès", "Oujda",
            "Kénitra", "Tétouan", "Salé", "Témara", "El Jadida", "Nador", "Béni Mellal", "Laâyoune",
        ];
    }
}
