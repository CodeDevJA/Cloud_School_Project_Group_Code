using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KonferenscentrumVast.Data;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KonferenscentrumVast.Repository.Implementations
{
    public class FileStorageRepository : IFileStorageRepository
    {
        private readonly AzureStorageConfig _storageConfig;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileStorageRepository> _logger;
        private BlobServiceClient _blobServiceClient = null!;

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

        public async Task<string> UploadToBlobStorageAsync(IFormFile file, string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

                var blobUploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                };

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

        public async Task<bool> DeleteFromBlobStorageAsync(string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

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

        public async Task<Stream> DownloadFromBlobStorageAsync(string secureFileName, string containerName)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(secureFileName);

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

        public async Task<UploadFile?> GetFileMetadataAsync(string secureFileName)
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
    }
}
