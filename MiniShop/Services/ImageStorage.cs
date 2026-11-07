namespace MiniShop.Services
{
    // Saves product photos to wwwroot/images/uploads with unique names.
    // Only files in uploads/ are ever deleted, so the shipped product images are safe.
    public class ImageStorage
    {
        public const long MaxBytes = 5 * 1024 * 1024;
        public const string Accept = ".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp";
        private const string UploadFolder = "uploads";

        private static readonly Dictionary<string, string> ExtensionByType = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "jpeg",
            [".jpeg"] = "jpeg",
            [".png"] = "png",
            [".webp"] = "webp",
        };

        private readonly string imagesRoot;
        private readonly ILogger<ImageStorage> logger;

        public ImageStorage(IWebHostEnvironment env, ILogger<ImageStorage> logger)
        {
            imagesRoot = Path.Combine(env.WebRootPath, "images");
            this.logger = logger;
        }

        // Returns an error message for the form, or null when the file is acceptable.
        public string? Validate(IFormFile file)
        {
            if (file.Length == 0) return "Le fichier est vide.";
            if (file.Length > MaxBytes) return $"L'image dépasse {MaxBytes / 1024 / 1024} Mo.";

            var ext = Path.GetExtension(file.FileName);
            if (!ExtensionByType.TryGetValue(ext, out var expected))
                return "Formats acceptés : JPG, PNG ou WebP.";

            if (DetectType(file) != expected)
                return "Le contenu du fichier ne correspond pas à une image JPG, PNG ou WebP.";

            return null;
        }

        // Saves a validated file and returns its path relative to wwwroot/images.
        public async Task<string> SaveAsync(IFormFile file, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext == ".jpeg") ext = ".jpg";

            var folder = Path.Combine(imagesRoot, UploadFolder);
            Directory.CreateDirectory(folder);

            var name = $"{Guid.NewGuid():N}{ext}";
            await using var stream = new FileStream(Path.Combine(folder, name), FileMode.CreateNew);
            await file.CopyToAsync(stream, ct);

            return $"{UploadFolder}/{name}";
        }

        public void DeleteIfUploaded(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            if (!relativePath.StartsWith(UploadFolder + "/", StringComparison.Ordinal)) return;

            var full = Path.GetFullPath(Path.Combine(imagesRoot, relativePath));
            var uploads = Path.GetFullPath(Path.Combine(imagesRoot, UploadFolder)) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(uploads, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                if (File.Exists(full)) File.Delete(full);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Could not delete replaced image {Path}", full);
            }
        }

        // Reads the first bytes: the extension and the browser's content type are both client-controlled.
        private static string? DetectType(IFormFile file)
        {
            Span<byte> head = stackalloc byte[12];
            using var s = file.OpenReadStream();
            var read = s.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
            if (read < 12) return null;

            if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return "jpeg";
            if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47) return "png";
            if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' &&
                head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P') return "webp";
            return null;
        }
    }
}
