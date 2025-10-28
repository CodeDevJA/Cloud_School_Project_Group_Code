using System.ComponentModel.DataAnnotations;

namespace KonferenscentrumVast.Models
{
    public class UploadFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string SecureFileName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [StringLength(10)]
        public string FileExtension { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string StorageUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContainerName { get; set; } = string.Empty;

        public int? BookingId { get; set; }
        public int? FacilityId { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [StringLength(64)]
        public string? FileHash { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // REMOVED: Description property since it wasn't in original model
        // public string? Description { get; set; }

        public UploadFile()
        {
            UploadedAt = DateTime.UtcNow;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public bool IsImage()
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            return imageExtensions.Contains(FileExtension.ToLower());
        }

        public bool IsContract()
        {
            var documentExtensions = new[] { ".pdf", ".doc", ".docx" };
            return documentExtensions.Contains(FileExtension.ToLower());
        }
    }
}
