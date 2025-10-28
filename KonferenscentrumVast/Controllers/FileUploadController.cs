using KonferenscentrumVast.DTOs;
using KonferenscentrumVast.Exceptions;
using KonferenscentrumVast.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KonferenscentrumVast.Controllers
{
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
            catch (FileValidationException ex) // FIXED: Use FileValidationException
            {
                _logger.LogWarning(ex, "File not found for download: {SecureFileName}", secureFileName);
                return NotFound(new { message = ex.UserFriendlyMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file download: {SecureFileName}", secureFileName);
                return StatusCode(500, new { message = "An unexpected error occurred during file download." });
            }
        }

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
            catch (FileValidationException ex) // FIXED: Use FileValidationException
            {
                _logger.LogWarning(ex, "File not found for deletion: {SecureFileName}", secureFileName);
                return NotFound(new FileDeleteResponseDto(
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
                    OriginalFileName = f.OriginalFileName ?? string.Empty,
                    FileType = f.FileType,
                    FileSize = f.FileSize,
                    FileExtension = f.FileExtension,
                    UploadedAt = f.UploadedAt,
                    BookingId = f.BookingId,
                    FacilityId = f.FacilityId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for booking: {BookingId}", bookingId);
                return StatusCode(500, new { message = "Error retrieving booking files" });
            }
        }

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
                    OriginalFileName = f.OriginalFileName ?? string.Empty,
                    FileType = f.FileType,
                    FileSize = f.FileSize,
                    FileExtension = f.FileExtension,
                    UploadedAt = f.UploadedAt,
                    BookingId = f.BookingId,
                    FacilityId = f.FacilityId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for facility: {FacilityId}", facilityId);
                return StatusCode(500, new { message = "Error retrieving facility files" });
            }
        }

        [HttpGet("health")]
        public ActionResult HealthCheck()
        {
            try
            {
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

        private string GetCurrentUserId()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value
                      ?? "system";

            _logger.LogDebug("Current user ID for file operation: {UserId}", userId);
            return userId;
        }
    }
}
