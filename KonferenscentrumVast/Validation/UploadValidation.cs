using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using KonferenscentrumVast.Exceptions;

namespace KonferenscentrumVast.Validation
{
    // Enkel valideringsklass (Validation layer)
    public class UploadValidation
    {
        // Acceptable MIME types (kan konfigureras)
        private static readonly HashSet<string> AllowedContentTypes = new()
        {
            "image/png",
            "image/jpeg",
            "application/pdf",
            "text/plain"
            // lägg till fler vid behov
        };

        private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB, justera vid behov

        public (bool IsValid, string? ErrorMessage) Validate(IFormFile file)
        {
            if (file == null) return (false, "No file provided");

            if (file.Length == 0) return (false, "File is empty");

            if (file.Length > MaxFileSizeBytes) return (false, $"File exceeds max size of {MaxFileSizeBytes} bytes");

            if (!AllowedContentTypes.Contains(file.ContentType ?? ""))
            {
                return (false, $"Content type '{file.ContentType}' is not allowed");
            }

            // Ytterligare checks kan läggas till här (ex. magic number/content sniffing)
            return (true, null);
        }
    }
}
