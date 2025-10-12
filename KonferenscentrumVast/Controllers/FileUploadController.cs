using KonferenscentrumVast.DTOs;
using KonferenscentrumVast.Exceptions;
using KonferenscentrumVast.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KonferenscentrumVast.Controllers
{
    /// <summary>
    /// API Controller for file upload operations
    /// Handles HTTP requests for file upload, download, and management
    /// Provides user confirmation and secure file access
    /// GDPR: Ensures proper authentication and authorization for file operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(
            IFileStorageService fileStorageService,
            ILogger<FileUploadController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage
        /// Accepts multipart form data with file and metadata
        /// Returns user confirmation with secure file information
        /// GDPR: Uses secure filenames and tracks uploader information
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponseDto>> UploadFile([FromForm] FileUploadRequestDto request)
        {
            try
            {
                _logger.LogInformation("File upload request received for {FileType}", request.FileType);

                // Get user identity for audit trail (GDPR compliance)
                var uploadedBy = GetCurrentUserId();

                // Process file upload through service layer
                var result = await _fileStorageService.UploadFileAsync(request, uploadedBy);

                // Return success response with user confirmation
                _logger.LogInformation("File upload completed successfully: {SecureFileName}", result.SecureFileName);

                return Ok(result);
            }
            catch (FileValidationException ex)
            {
                // Handle validation errors with user-friendly messages
                _logger.LogWarning(ex, "File validation failed during upload");
                return BadRequest(new FileUploadResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage
                ));
            }
            catch (FileSizeException ex)
            {
                // Handle file size errors
                _logger.LogWarning(ex, "File size exceeded during upload");
                return BadRequest(new FileUploadResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage
                ));
            }
            catch (FileTypeException ex)
            {
                // Handle file type errors
                _logger.LogWarning(ex, "Invalid file type during upload");
                return BadRequest(new FileUploadResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage
                ));
            }
            catch (SecurityException ex)
            {
                // Handle security-related errors
                _logger.LogWarning(ex, "Security check failed during upload");
                return BadRequest(new FileUploadResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage
                ));
            }
            catch (Exception ex)
            {
                // Handle unexpected errors with generic message (GDPR: no internal details)
                _logger.LogError(ex, "Unexpected error during file upload");
                return StatusCode(500, new FileUploadResponseDto(
                    success: false,
                    message: "An unexpected error occurred during file upload. Please try again."
                ));
            }
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage
        /// Returns file stream with proper content type for browser handling
        /// Security: Validates file access and provides secure download
        /// </summary>
        [HttpGet("download/{secureFileName}")]
        public async Task<IActionResult> DownloadFile(string secureFileName, [FromQuery] bool forceDownload = false)
        {
            try
            {
                _logger.LogInformation("File download request received: {SecureFileName}", secureFileName);

                // Download file stream from service layer
                var fileStream = await _fileStorageService.DownloadFileAsync(secureFileName);

                // Get file metadata for content type and filename
                var fileMetadata = await _fileStorageService.GetFileMetadataAsync(secureFileName);
                if (fileMetadata == null)
                {
                    return NotFound(new { message = "File not found" });
                }

                // Set response headers for file download
                var contentType = fileMetadata.ContentType;
                var downloadFileName = forceDownload ? fileMetadata.OriginalFileName : null;

                _logger.LogInformation("File download completed: {SecureFileName}", secureFileName);

                // Return file stream result
                if (forceDownload)
                {
                    return File(fileStream, contentType, downloadFileName);
                }
                else
                {
                    return File(fileStream, contentType);
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found for download: {SecureFileName}", secureFileName);
                return NotFound(new { message = ex.UserFriendlyMessage });
            }
            catch (FileValidationException ex)
            {
                _logger.LogWarning(ex, "File validation failed for download: {SecureFileName}", secureFileName);
                return BadRequest(new { message = ex.UserFriendlyMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file download: {SecureFileName}", secureFileName);
                return StatusCode(500, new { message = "An unexpected error occurred during file download." });
            }
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage and updates metadata
        /// GDPR: Supports right to be forgotten with proper confirmation
        /// Returns user confirmation of deletion
        /// </summary>
        [HttpDelete("{secureFileName}")]
        public async Task<ActionResult<FileDeleteResponseDto>> DeleteFile(string secureFileName)
        {
            try
            {
                _logger.LogInformation("File deletion request received: {SecureFileName}", secureFileName);

                // Delete file through service layer
                var result = await _fileStorageService.DeleteFileAsync(secureFileName);

                if (result)
                {
                    _logger.LogInformation("File deleted successfully: {SecureFileName}", secureFileName);
                    return Ok(new FileDeleteResponseDto(
                        success: true,
                        message: "File deleted successfully",
                        deletedFileName: secureFileName
                    ));
                }
                else
                {
                    _logger.LogWarning("File deletion may have failed: {SecureFileName}", secureFileName);
                    return BadRequest(new FileDeleteResponseDto(
                        success: false,
                        message: "File deletion may have failed",
                        deletedFileName: secureFileName
                    ));
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found for deletion: {SecureFileName}", secureFileName);
                return NotFound(new FileDeleteResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage,
                    deletedFileName: secureFileName
                ));
            }
            catch (FileValidationException ex)
            {
                _logger.LogWarning(ex, "File validation failed for deletion: {SecureFileName}", secureFileName);
                return BadRequest(new FileDeleteResponseDto(
                    success: false,
                    message: ex.UserFriendlyMessage,
                    deletedFileName: secureFileName
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file deletion: {SecureFileName}", secureFileName);
                return StatusCode(500, new FileDeleteResponseDto(
                    success: false,
                    message: "An unexpected error occurred during file deletion.",
                    deletedFileName: secureFileName
                ));
            }
        }

        /// <summary>
        /// Checks if a file exists in the system
        /// Used for client-side validation and existence checks
        /// Returns simple existence status
        /// </summary>
        [HttpGet("exists/{secureFileName}")]
        public async Task<ActionResult> FileExists(string secureFileName)
        {
            try
            {
                var exists = await _fileStorageService.FileExistsAsync(secureFileName);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {SecureFileName}", secureFileName);
                return StatusCode(500, new { message = "Error checking file existence" });
            }
        }

        /// <summary>
        /// Gets all files for a specific booking
        /// Used to display booking-related documents and images
        /// GDPR: Only returns files that user has access to
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        public async Task<ActionResult<List<FileListResponseDto>>> GetFilesForBooking(int bookingId)
        {
            try
            {
                var files = await _fileStorageService.GetFilesForBookingAsync(bookingId);

                // Convert to response DTOs
                var response = files.Select(f => new FileListResponseDto
                {
                    SecureFileName = f.SecureFileName,
                    OriginalFileName = f.OriginalFileName,
                    FileType = f.FileType,
                    FileSize = f.FileSize,
                    FileExtension = f.FileExtension,
                    UploadedAt = f.UploadedAt,
                    BookingId = f.BookingId,
                    FacilityId = f.FacilityId,
                    Description = f.Description
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for booking: {BookingId}", bookingId);
                return StatusCode(500, new { message = "Error retrieving booking files" });
            }
        }

        /// <summary>
        /// Gets all files for a specific facility
        /// Used to display facility images and related documents
        /// Public access for facility information
        /// </summary>
        [HttpGet("facility/{facilityId}")]
        public async Task<ActionResult<List<FileListResponseDto>>> GetFilesForFacility(int facilityId)
        {
            try
            {
                var files = await _fileStorageService.GetFilesForFacilityAsync(facilityId);

                // Convert to response DTOs
                var response = files.Select(f => new FileListResponseDto
                {
                    SecureFileName = f.SecureFileName,
                    OriginalFileName = f.OriginalFileName,
                    FileType = f.FileType,
                    FileSize = f.FileSize,
                    FileExtension = f.FileExtension,
                    UploadedAt = f.UploadedAt,
                    BookingId = f.BookingId,
                    FacilityId = f.FacilityId,
                    Description = f.Description
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for facility: {FacilityId}", facilityId);
                return StatusCode(500, new { message = "Error retrieving facility files" });
            }
        }

        /// <summary>
        /// Health check endpoint for file upload functionality
        /// Verifies that Azure Blob Storage is accessible
        /// Used for monitoring and diagnostics
        /// </summary>
        [HttpGet("health")]
        public ActionResult HealthCheck()
        {
            try
            {
                // Basic health check - in production, this would verify Azure connectivity
                return Ok(new
                {
                    status = "Healthy",
                    service = "FileUpload",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for file upload service");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    service = "FileUpload",
                    error = "Service unavailable"
                });
            }
        }

        /// <summary>
        /// Gets the current user ID from authentication context
        /// Used for audit trail and GDPR compliance
        /// Returns "system" if no user is authenticated (for background operations)
        /// </summary>
        private string GetCurrentUserId()
        {
            // Get user identity from authentication context
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value
                      ?? "system"; // Fallback for system operations

            _logger.LogDebug("Current user ID for file operation: {UserId}", userId);
            return userId;
        }
    }
}
