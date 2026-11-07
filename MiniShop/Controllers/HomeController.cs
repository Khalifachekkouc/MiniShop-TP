using Microsoft.AspNetCore.Mvc;
using MiniShop.Models;
using System.Diagnostics;

namespace MiniShop.Controllers
{
    // Error pages: unhandled exceptions (outside Development) and status codes such as 404.
    public class HomeController : Controller
    {
        // Re-executed by UseStatusCodePagesWithReExecute for 404s and other error codes.
        [Route("Home/StatusCode/{code:int}")]
        public IActionResult StatusPage(int code)
        {
            return View("StatusCode", code);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
