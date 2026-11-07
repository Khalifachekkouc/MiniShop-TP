using Microsoft.AspNetCore.Mvc;
using MiniShop.Services;
using MiniShop.ViewModels;

namespace MiniShop.Controllers
{
    [Route("panier")]
    public class CartController : Controller
    {
        private readonly CartService cart;

        public CartController(CartService cart)
        {
            this.cart = cart;
        }

        // site.js posts the add-to-cart forms with fetch and this header, and gets JSON back.
        private bool IsFetch => Request.Headers["X-Requested-With"] == "fetch";

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            return View(await cart.LoadAsync());
        }

        // intent=buy ("Acheter maintenant") goes straight to the payment page.
        // colorId / storageId come from the product page; without them the defaults are used.
        [HttpPost("ajouter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int? colorId, int? storageId, int quantity = 1, string? intent = null, string? returnUrl = null)
        {
            var outcome = await cart.AddAsync(id, colorId, storageId, quantity);
            var product = outcome.Product;
            var label = product == null ? "" : outcome.Options == "" ? $"« {product.Name} »" : $"« {product.Name} » ({outcome.Options})";
            var ok = outcome.Result is AddResult.Added or AddResult.Clamped;
            var message = outcome.Result switch
            {
                AddResult.Added => $"{label} a été ajouté au panier.",
                AddResult.Clamped => $"{label} : quantité limitée à {outcome.Quantity} dans le panier.",
                AddResult.OutOfStock => $"{label} est épuisé.",
                AddResult.InvalidOption => "Cette couleur ou cette capacité n'existe pas pour ce produit.",
                _ => "Ce produit n'existe plus.",
            };

            if (IsFetch)
            {
                // A tampered option is a client error: 400 with the message, never a server error.
                if (outcome.Result == AddResult.InvalidOption) Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json(new { ok, message, count = cart.Count() });
            }

            TempData[ok ? Toast.Success : Toast.Error] = message;
            if (ok && intent == "buy") return RedirectToAction("Index", "Checkout");
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        [HttpPost("modifier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string line, int quantity)
        {
            await cart.SetAsync(line ?? "", quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("retirer")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(string line)
        {
            cart.Remove(line ?? "");
            TempData[Toast.Success] = "L'article a été retiré du panier.";
            return RedirectToAction(nameof(Index));
        }
    }
}
