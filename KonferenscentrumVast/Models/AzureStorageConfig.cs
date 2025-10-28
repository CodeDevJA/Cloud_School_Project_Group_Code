using System.ComponentModel.DataAnnotations;

namespace KonferenscentrumVast.Models
{
    /// <summary>
    /// Configuration model for Azure Storage settings
    /// Binds to AzureStorage section in appsettings.json
    /// </summary>
    public class AzureStorageConfig
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty; // FIXED: Add = string.Empty

        public string ContainerName { get; set; } = "documents";

        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    }
}
