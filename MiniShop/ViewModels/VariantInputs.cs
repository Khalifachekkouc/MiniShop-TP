namespace MiniShop.ViewModels
{
    // Rows posted by the admin form's « Couleurs » and « Stockage » sections, bound as
    // Colors[key].Name (+ Colors.Index = key), so rows can be added, removed and reordered in the page.
    // Posted order is the display order. Ids are checked against the product, never trusted.
    public class ColorInput
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Hex { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public class StorageInput
    {
        public int? Id { get; set; }
        public string? Label { get; set; }
        public decimal? Price { get; set; }
    }

    // One row of the admin form (the key is the Colors.Index / Storages.Index value).
    public record ColorRow(string Key, int? Id, string Name, string Hex, string? ImageUrl);
    public record StorageRow(string Key, int? Id, string Label, decimal? Price);
}
