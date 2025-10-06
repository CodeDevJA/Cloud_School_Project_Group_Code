using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonferenscentrumVast.Models
{
    // Entity / Model för upload metadata (UploadFile entity)
    public class UploadFile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(1024)]
        public string OriginalFileName { get; set; } = null!; // original filename from client

        [Required]
        [MaxLength(1024)]
        public string StoredFileName { get; set; } = null!;   // filename we save in Blob (unique)

        [Required]
        [MaxLength(256)]
        public string ContentType { get; set; } = null!;

        public long Size { get; set; }                         // size in bytes

        [Required]
        public string BlobUrl { get; set; } = null!;           // URL to blob in Azure Storage

        public string? UploadedBy { get; set; }                // optional user id / name

        public DateTimeOffset UploadedAt { get; set; }         // UTC timestamp

        [MaxLength(128)]
        public string? Checksum { get; set; }                  // optional checksum (e.g. SHA256)
    }

    // DTO för inkommande request (UploadRequest DTO)
    public class UploadRequest
    {
        // IFormFile hanteras i Controller; DTO inkluderar här metadatafält om behövs
        public string? UploadedBy { get; set; }
    }

    // DTO för svar (UploadResponse DTO)
    public class UploadResponse
    {
        public Guid Id { get; set; }
        public string BlobUrl { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public long Size { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
