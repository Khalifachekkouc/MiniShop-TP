using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Services;
using MiniShop.ViewModels;

namespace MiniShop.Controllers
{
    // Payment page: a visual mock. Nothing is charged, no order is stored, stock is not touched.
    [Route("paiement")]
    public class CheckoutController : Controller
    {
        private const string ConfirmationKey = "order.confirmation";
        private readonly CartService cart;

        public CheckoutController(CartService cart)
        {
            this.cart = cart;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var view = await cart.LoadAsync();
            if (view.IsEmpty) return RedirectToAction("Index", "Cart");
            return View(new CheckoutViewModel { Form = new CheckoutForm(), Cart = view });
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "Form")] CheckoutForm form)
        {
            var view = await cart.LoadAsync();
            if (view.IsEmpty)
            {
                TempData[Toast.Error] = "Votre panier est vide.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid) return View(new CheckoutViewModel { Form = form, Cart = view });

            var confirmation = new OrderConfirmation(
                NewOrderNumber(), form.FullName.Trim(), form.Email.Trim(), form.City.Trim(),
                form.PaymentMethod, view.ItemCount, view.Total,
                view.Lines.Select(l => new OrderLine(l.Product.Name, l.Options, l.Quantity, l.LineTotal)).ToList());
            cart.Clear();

            TempData[ConfirmationKey] = JsonSerializer.Serialize(confirmation);
            return RedirectToAction(nameof(Confirmation));
        }

        [HttpGet("confirmation")]
        public IActionResult Confirmation()
        {
            if (TempData[ConfirmationKey] is not string json) return RedirectToAction("Index", "Shop");
            var confirmation = JsonSerializer.Deserialize<OrderConfirmation>(json);
            return confirmation == null ? RedirectToAction("Index", "Shop") : View(confirmation);
        }

        // e.g. MS-261107-4821
        private static string NewOrderNumber() =>
            $"MS-{DateTime.Now:yyMMdd}-{RandomNumberGenerator.GetInt32(1000, 10000)}";
    }
}
