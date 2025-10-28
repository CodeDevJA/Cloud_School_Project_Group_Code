using System.ComponentModel.DataAnnotations;

namespace KonferenscentrumVast.DTOs
{
    public class FileUploadRequestDto
    {
        [Required(ErrorMessage = "File is required for upload")]
        public IFormFile File { get; set; } = null!;

        [Required(ErrorMessage = "File type must be specified")]
        [RegularExpression("^(contract|image)$", ErrorMessage = "File type must be 'contract' or 'image'")]
        public string FileType { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Booking ID must be positive")]
        public int? BookingId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Facility ID must be positive")]
        public int? FacilityId { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class FileUploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string SecureFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public FileUploadResponseDto(bool success, string message, string secureFileName,
                                   string fileUrl, long fileSize, string fileType)
        {
            Success = success;
            Message = message;
            SecureFileName = secureFileName;
            FileUrl = fileUrl;
            FileSize = fileSize;
            FileType = fileType;
            UploadedAt = DateTime.UtcNow;
        }

        public FileUploadResponseDto(bool success, string message)
        {
            Success = success;
            Message = message;
            UploadedAt = DateTime.UtcNow;
        }
    }

    public class FileDeleteRequestDto
    {
        [Required(ErrorMessage = "Secure file name is required for deletion")]
        [StringLength(255, ErrorMessage = "File name is too long")]
        public string SecureFileName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string? Reason { get; set; }
    }

    public class FileDeleteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeletedFileName { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }

        public FileDeleteResponseDto(bool success, string message, string deletedFileName)
        {
            Success = success;
            Message = message;
            DeletedFileName = deletedFileName;
            DeletedAt = DateTime.UtcNow;
        }
    }

    public class FileListResponseDto
    {
        public string SecureFileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public int? BookingId { get; set; }
        public int? FacilityId { get; set; }
    }

    public class FileDownloadRequestDto
    {
        [Required(ErrorMessage = "Secure file name is required for download")]
        [StringLength(255, ErrorMessage = "File name is too long")]
        public string SecureFileName { get; set; } = string.Empty;
        public bool ForceDownload { get; set; } = false;
    }
}
