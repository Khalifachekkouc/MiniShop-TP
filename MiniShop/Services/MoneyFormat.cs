using System.Globalization;

namespace MiniShop.Services
{
    public static class MoneyFormat
    {
        private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        // 13990 -> "13 990 MAD", 349.5 -> "349,50 MAD"
        public static string Mad(decimal amount)
        {
            var format = decimal.Truncate(amount) == amount ? "N0" : "N2";
            return amount.ToString(format, French) + " MAD";
        }

        public static string Count(int n) => n.ToString("N0", French);
    }
}
