using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KonferenscentrumVast.DTOs;
using KonferenscentrumVast.Exceptions;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Validation;
using Microsoft.Extensions.Options;

namespace KonferenscentrumVast.Services
{
    /// <summary>
    /// Service layer for file storage operations with Azure Blob Storage
    /// Handles business logic for file upload, download, and deletion
    /// GDPR: Ensures secure file handling and proper metadata management
    /// </summary>
    public interface IFileStorageService
    {
        Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, string uploadedBy);
        Task<bool> DeleteFileAsync(string secureFileName);
        Task<Stream> DownloadFileAsync(string secureFileName);
        Task<bool> FileExistsAsync(string secureFileName);
    }

    /// <summary>
    /// Implementation of file storage service using Azure Blob Storage
    /// Separates business logic from storage infrastructure details
    /// Uses secure practices for all file operations
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly AzureStorageConfig _storageConfig;
        private readonly UploadFileValidator _validator;
        private readonly ILogger<FileStorageService> _logger;
        private readonly IFileStorageRepository _fileStorageRepository;

        public FileStorageService(
            IOptions<AzureStorageConfig> storageConfig,
            UploadFileValidator validator,
            ILogger<FileStorageService> logger,
            IFileStorageRepository fileStorageRepository)
        {
            _storageConfig = storageConfig.Value;
            _validator = validator;
            _logger = logger;
            _fileStorageRepository = fileStorageRepository;
        }

        /// <summary>
        /// Main method for uploading files to Azure Blob Storage
        /// Performs validation, secure filename generation, and metadata storage
        /// GDPR: Uses secure filenames and tracks upload information
        /// </summary>
        public async Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, string uploadedBy)
        {
            try
            {
                _logger.LogInformation("Starting file upload process for {FileName}", request.File.FileName);

                // Step 1: Validate the upload request
                _validator.ValidateUploadRequest(request);

                // Step 2: Generate secure filename (GDPR compliant)
                var secureFileName = _validator.GenerateSecureFileName(request.File.FileName, request.FileType);

                // Step 3: Upload file to Azure Blob Storage via repository
                var fileUrl = await _fileStorageRepository.UploadToBlobStorageAsync(
                    request.File,
                    secureFileName,
                    _storageConfig.ContainerName
                );

                // Step 4: Create file metadata record in database
                var uploadFile = new UploadFile
                {
                    SecureFileName = secureFileName,
                    OriginalFileName = request.File.FileName,
                    FileType = request.FileType,
                    FileSize = request.File.Length,
                    FileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant(),
                    StorageUrl = fileUrl,
                    ContainerName = _storageConfig.ContainerName,
                    BookingId = request.BookingId,
                    FacilityId = request.FacilityId,
                    ContentType = request.File.ContentType,
                    UploadedBy = uploadedBy,
                    FileHash = CalculateFileHash(request.File) // For integrity verification
                };

                // Save metadata to database via repository
                await _fileStorageRepository.SaveFileMetadataAsync(uploadFile);

                // Step 5: Log successful upload and return response
                _logger.LogInformation(
                    "File uploaded successfully: {SecureFileName} (Original: {OriginalFileName})",
                    secureFileName,
                    request.File.FileName
                );

                return new FileUploadResponseDto(
                    success: true,
                    message: "File uploaded successfully and is now securely stored",
                    secureFileName: secureFileName,
                    fileUrl: fileUrl,
                    fileSize: request.File.Length,
                    fileType: request.FileType
                );
            }
            catch (UploadFileException ex)
            {
                // Re-throw custom exceptions with proper context
                _logger.LogWarning(ex, "File upload validation failed: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors and wrap in storage exception
                _logger.LogError(ex, "Unexpected error during file upload for {FileName}", request.File.FileName);
                throw new StorageException("File upload operation", ex);
            }
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage and updates metadata
        /// GDPR: Supports right to be forgotten with proper audit trail
        /// </summary>
        public async Task<bool> DeleteFileAsync(string secureFileName)
        {
            try
            {
                _logger.LogInformation("Starting file deletion process for {SecureFileName}", secureFileName);

                // Step 1: Validate deletion request
                _validator.ValidateDeleteRequest(secureFileName);

                // Step 2: Get file metadata from database
                var fileMetadata = await _fileStorageRepository.GetFileMetadataAsync(secureFileName);
                if (fileMetadata == null)
                {
                    _logger.LogWarning("File metadata not found for deletion: {SecureFileName}", secureFileName);
                    throw new FileNotFoundException(secureFileName);
                }

                // Step 3: Mark file as deleted in database (soft delete for GDPR)
                fileMetadata.MarkAsDeleted();
                await _fileStorageRepository.UpdateFileMetadataAsync(fileMetadata);

                // Step 4: Delete actual file from Azure Blob Storage
                var deletionResult = await _fileStorageRepository.DeleteFromBlobStorageAsync(
                    secureFileName,
                    _storageConfig.ContainerName
                );

                if (deletionResult)
                {
                    _logger.LogInformation("File deleted successfully: {SecureFileName}", secureFileName);
                }
                else
                {
                    _logger.LogWarning("File deletion may have failed: {SecureFileName}", secureFileName);
                }

                return deletionResult;
            }
            catch (UploadFileException ex)
            {
                _logger.LogWarning(ex, "File deletion validation failed: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file deletion for {SecureFileName}", secureFileName);
                throw new StorageException("File deletion operation", ex);
            }
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage as a stream
        /// Used for both display and download operations
        /// Security: Validates file existence and access rights
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string secureFileName)
        {
            try
            {
                _logger.LogInformation("Starting file download process for {SecureFileName}", secureFileName);

                // Step 1: Validate download request
                _validator.ValidateDownloadRequest(secureFileName);

                // Step 2: Check if file exists in database (not deleted)
                var fileMetadata = await _fileStorageRepository.GetFileMetadataAsync(secureFileName);
                if (fileMetadata == null || fileMetadata.IsDeleted)
                {
                    _logger.LogWarning("File not found or already deleted: {SecureFileName}", secureFileName);
                    throw new FileNotFoundException(secureFileName);
                }

                // Step 3: Download file from Azure Blob Storage
                var fileStream = await _fileStorageRepository.DownloadFromBlobStorageAsync(
                    secureFileName,
                    _storageConfig.ContainerName
                );

                _logger.LogInformation("File downloaded successfully: {SecureFileName}", secureFileName);
                return fileStream;
            }
            catch (UploadFileException ex)
            {
                _logger.LogWarning(ex, "File download validation failed: {ErrorMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file download for {SecureFileName}", secureFileName);
                throw new StorageException("File download operation", ex);
            }
        }

        /// <summary>
        /// Checks if a file exists in Azure Blob Storage
        /// Used for validation and existence checks before operations
        /// </summary>
        public async Task<bool> FileExistsAsync(string secureFileName)
        {
            try
            {
                // Check both database metadata and blob storage
                var fileMetadata = await _fileStorageRepository.GetFileMetadataAsync(secureFileName);
                if (fileMetadata == null || fileMetadata.IsDeleted)
                {
                    return false;
                }

                return await _fileStorageRepository.BlobExistsAsync(secureFileName, _storageConfig.ContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence for {SecureFileName}", secureFileName);
                return false;
            }
        }

        /// <summary>
        /// Calculates a simple hash for file integrity verification
        /// GDPR: Used to ensure file hasn't been tampered with
        /// Note: For production, consider stronger hashing algorithms
        /// </summary>
        private string CalculateFileHash(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Gets file metadata for a specific secure filename
        /// Used by controllers to provide file information
        /// </summary>
        public async Task<UploadFile> GetFileMetadataAsync(string secureFileName)
        {
            return await _fileStorageRepository.GetFileMetadataAsync(secureFileName);
        }

        /// <summary>
        /// Gets all files for a specific booking
        /// Used to display all files related to a booking
        /// GDPR: Only returns files that haven't been soft-deleted
        /// </summary>
        public async Task<List<UploadFile>> GetFilesForBookingAsync(int bookingId)
        {
            return await _fileStorageRepository.GetFilesForBookingAsync(bookingId);
        }

        /// <summary>
        /// Gets all files for a specific facility
        /// Used to display facility images and documents
        /// GDPR: Only returns files that haven't been soft-deleted
        /// </summary>
        public async Task<List<UploadFile>> GetFilesForFacilityAsync(int facilityId)
        {
            return await _fileStorageRepository.GetFilesForFacilityAsync(facilityId);
        }
    }
}
