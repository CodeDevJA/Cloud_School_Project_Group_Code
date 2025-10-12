using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KonferenscentrumVast.Data;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KonferenscentrumVast.Repository.Implementations
{
    /// <summary>
    /// Repository implementation for file storage operations
    /// Handles Azure Blob Storage for files and PostgreSQL for metadata
    /// Separates data access logic from business logic
    /// </summary>
    public class FileStorageRepository : IFileStorageRepository
    {
        private readonly AzureStorageConfig _storageConfig;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileStorageRepository> _logger;
        private BlobServiceClient _blobServiceClient;

        public FileStorageRepository(
            IOptions<AzureStorageConfig> storageConfig,
            ApplicationDbContext context,
            ILogger<FileStorageRepository> logger)
        {
            _storageConfig = storageConfig.Value;
            _context = context;
            _logger = logger;
            InitializeBlobServiceClient();
        }

        /// <summary>
        /// Initializes the Azure Blob Service client using connection string
        /// Creates blob container if it doesn't exist
        /// </summary>
        private void InitializeBlobServiceClient()
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(_storageConfig.ConnectionString);
                _logger.LogInformation("Azure Blob Service client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Blob Service client");
                throw;
            }
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage
        /// Uses secure filename and sets proper content type
        /// Returns the full URL of the uploaded blob
        /// </summary>
        public async Task<string> UploadToBlobStorageAsync(IFormFile file, string secureFileName, string containerName)
        {
            try
            {
                // Get blob container client and create container if it doesn't exist
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Get blob client for the specific file
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

                // Set blob upload options with content type
                var blobUploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                };

                // Upload file stream to blob storage
                using var fileStream = file.OpenReadStream();
                var response = await blobClient.UploadAsync(fileStream, blobUploadOptions);

                _logger.LogDebug("File uploaded to blob storage: {BlobUri}", blobClient.Uri);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to blob storage: {FileName}", secureFileName);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage
        /// Returns true if deletion was successful or file didn't exist
        /// </summary>
        public async Task<bool> DeleteFromBlobStorageAsync(string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

                // Delete blob if it exists
                var response = await blobClient.DeleteIfExistsAsync();

                _logger.LogDebug("File deletion from blob storage: {FileName} - Success: {Success}",
                    secureFileName, response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from blob storage: {FileName}", secureFileName);
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage as a stream
        /// Used for both file downloads and content delivery
        /// </summary>
        public async Task<Stream> DownloadFromBlobStorageAsync(string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

                // Download blob content to stream
                var response = await blobClient.DownloadContentAsync();

                _logger.LogDebug("File downloaded from blob storage: {FileName}", secureFileName);

                return response.Value.Content.ToStream();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from blob storage: {FileName}", secureFileName);
                throw;
            }
        }

        /// <summary>
        /// Checks if a blob exists in Azure Blob Storage
        /// Used for validation before operations
        /// </summary>
        public async Task<bool> BlobExistsAsync(string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check blob existence: {FileName}", secureFileName);
                return false;
            }
        }

        /// <summary>
        /// Gets file metadata from PostgreSQL database by secure filename
        /// Returns null if file not found or is soft-deleted
        /// </summary>
        public async Task<UploadFile> GetFileMetadataAsync(string secureFileName)
        {
            try
            {
                return await _context.UploadFiles
                    .Where(f => f.SecureFileName == secureFileName && !f.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file metadata: {FileName}", secureFileName);
                throw;
            }
        }

        /// <summary>
        /// Saves file metadata to PostgreSQL database
        /// Creates new record with upload information
        /// </summary>
        public async Task<UploadFile> SaveFileMetadataAsync(UploadFile uploadFile)
        {
            try
            {
                _context.UploadFiles.Add(uploadFile);
                await _context.SaveChangesAsync();

                _logger.LogDebug("File metadata saved to database: {FileName}", uploadFile.SecureFileName);

                return uploadFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file metadata: {FileName}", uploadFile.SecureFileName);
                throw;
            }
        }

        /// <summary>
        /// Updates existing file metadata in PostgreSQL database
        /// Used for soft delete and metadata modifications
        /// </summary>
        public async Task<UploadFile> UpdateFileMetadataAsync(UploadFile uploadFile)
        {
            try
            {
                _context.UploadFiles.Update(uploadFile);
                await _context.SaveChangesAsync();

                _logger.LogDebug("File metadata updated in database: {FileName}", uploadFile.SecureFileName);

                return uploadFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update file metadata: {FileName}", uploadFile.SecureFileName);
                throw;
            }
        }

        /// <summary>
        /// Gets all non-deleted files for a specific booking
        /// Used to display booking-related documents
        /// </summary>
        public async Task<List<UploadFile>> GetFilesForBookingAsync(int bookingId)
        {
            try
            {
                return await _context.UploadFiles
                    .Where(f => f.BookingId == bookingId && !f.IsDeleted)
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get files for booking: {BookingId}", bookingId);
                throw;
            }
        }

        /// <summary>
        /// Gets all non-deleted files for a specific facility
        /// Used to display facility images and documents
        /// </summary>
        public async Task<List<UploadFile>> GetFilesForFacilityAsync(int facilityId)
        {
            try
            {
                return await _context.UploadFiles
                    .Where(f => f.FacilityId == facilityId && !f.IsDeleted)
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get files for facility: {FacilityId}", facilityId);
                throw;
            }
        }

        /// <summary>
        /// Performs soft delete on file metadata
        /// Marks file as deleted without removing from database
        /// GDPR: Maintains audit trail while supporting right to be forgotten
        /// </summary>
        public async Task<bool> SoftDeleteFileMetadataAsync(string secureFileName)
        {
            try
            {
                var fileMetadata = await GetFileMetadataAsync(secureFileName);
                if (fileMetadata == null)
                {
                    return false;
                }

                fileMetadata.MarkAsDeleted();
                await UpdateFileMetadataAsync(fileMetadata);

                _logger.LogDebug("File metadata soft deleted: {FileName}", secureFileName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete file metadata: {FileName}", secureFileName);
                throw;
            }
        }

        /// <summary>
        /// Gets all files (including deleted) for administrative purposes
        /// Used for audit and GDPR compliance reporting
        /// Should be restricted to admin users only
        /// </summary>
        public async Task<List<UploadFile>> GetAllFilesForAdminAsync()
        {
            try
            {
                return await _context.UploadFiles
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all files for admin");
                throw;
            }
        }
    }
}
