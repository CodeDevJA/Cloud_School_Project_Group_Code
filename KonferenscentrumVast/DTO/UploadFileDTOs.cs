using System.ComponentModel.DataAnnotations;

namespace KonferenscentrumVast.DTOs
{
    /// <summary>
    /// Request DTO for file upload operations from client to API
    /// Contains all data needed to securely upload and process files
    /// GDPR: No sensitive data in request, only file and metadata
    /// </summary>
    public class FileUploadRequestDto
    {
        /// <summary>
        /// The actual file being uploaded - from multipart form data
        /// Required field with client-side validation
        /// </summary>
        [Required(ErrorMessage = "File is required for upload")]
        public IFormFile File { get; set; }

        /// <summary>
        /// Type categorization: 'contract' or 'image'
        /// Determines storage location and processing rules
        /// GDPR: Used for appropriate access controls
        /// </summary>
        [Required(ErrorMessage = "File type must be specified")]
        [RegularExpression("^(contract|image)$", ErrorMessage = "File type must be 'contract' or 'image'")]
        public string FileType { get; set; }

        /// <summary>
        /// Optional reference to related booking ID
        /// Used to associate files with specific bookings
        /// GDPR: Only stored when necessary for business operations
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Booking ID must be positive")]
        public int? BookingId { get; set; }

        /// <summary>
        /// Optional reference to related facility ID
        /// Used for facility images and related documents
        /// GDPR: Only stored when necessary for business operations
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Facility ID must be positive")]
        public int? FacilityId { get; set; }

        /// <summary>
        /// Optional description for the uploaded file
        /// GDPR: User-provided metadata, stored as provided
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Response DTO for successful file upload operations
    /// Provides user confirmation and file access information
    /// GDPR: Returns secure URLs and system-generated filenames only
    /// </summary>
    public class FileUploadResponseDto
    {
        /// <summary>
        /// Indicates if upload was successful
        /// Used for client-side decision making
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// User-friendly confirmation message
        /// GDPR: Clear communication without technical details
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// System-generated secure filename (GDPR compliant)
        /// Format: {type}_{timestamp}_{guid}.{extension}
        /// Used for future file operations
        /// </summary>
        public string SecureFileName { get; set; }

        /// <summary>
        /// Full URL to access the uploaded file in Azure Blob Storage
        /// May require authentication/SAS tokens for actual access
        /// </summary>
        public string FileUrl { get; set; }

        /// <summary>
        /// File size in bytes for client information
        /// Can be formatted for display on client side
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File type for client-side processing
        /// Matches the requested file type from upload
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// UTC timestamp when file was uploaded
        /// Used for display and sorting on client side
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Constructor for successful upload responses
        /// Ensures consistent response structure
        /// </summary>
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

        /// <summary>
        /// Constructor for error responses
        /// Provides clear error information to client
        /// </summary>
        public FileUploadResponseDto(bool success, string message)
        {
            Success = success;
            Message = message;
            UploadedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// DTO for file deletion requests
    /// GDPR: Requires specific file identification for right to be forgotten
    /// </summary>
    public class FileDeleteRequestDto
    {
        /// <summary>
        /// System-generated secure filename to delete
        /// Required for precise file identification
        /// </summary>
        [Required(ErrorMessage = "Secure file name is required for deletion")]
        [StringLength(255, ErrorMessage = "File name is too long")]
        public string SecureFileName { get; set; }

        /// <summary>
        /// Reason for deletion (optional but recommended for audit)
        /// GDPR: Helps maintain audit trail for data processing
        /// </summary>
        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// DTO for file deletion responses
    /// Confirms deletion operation result to client
    /// </summary>
    public class FileDeleteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string DeletedFileName { get; set; }
        public DateTime DeletedAt { get; set; }

        public FileDeleteResponseDto(bool success, string message, string deletedFileName)
        {
            Success = success;
            Message = message;
            DeletedFileName = deletedFileName;
            DeletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// DTO for listing uploaded files with metadata
    /// Used for file management interfaces
    /// GDPR: Only returns non-sensitive file information
    /// </summary>
    public class FileListResponseDto
    {
        public string SecureFileName { get; set; }
        public string OriginalFileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string FileExtension { get; set; }
        public DateTime UploadedAt { get; set; }
        public int? BookingId { get; set; }
        public int? FacilityId { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// DTO for file download requests
    /// Used when client needs to download specific files
    /// </summary>
    public class FileDownloadRequestDto
    {
        [Required(ErrorMessage = "Secure file name is required for download")]
        [StringLength(255, ErrorMessage = "File name is too long")]
        public string SecureFileName { get; set; }

        /// <summary>
        /// Optional parameter to force download vs. display in browser
        /// Affects Content-Disposition header
        /// </summary>
        public bool ForceDownload { get; set; } = false;
    }
}
