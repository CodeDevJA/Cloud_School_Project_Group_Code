using KonferenscentrumVast.Models;

namespace KonferenscentrumVast.Repository.Interfaces
{
    /// <summary>
    /// Repository interface for file storage operations
    /// Abstracts the data access layer for file metadata and blob storage
    /// Follows repository pattern for testability and maintainability
    /// </summary>
    public interface IFileStorageRepository
    {
        // Azure Blob Storage Operations
        Task<string> UploadToBlobStorageAsync(IFormFile file, string secureFileName, string containerName);
        Task<bool> DeleteFromBlobStorageAsync(string secureFileName, string containerName);
        Task<Stream> DownloadFromBlobStorageAsync(string secureFileName, string containerName);
        Task<bool> BlobExistsAsync(string secureFileName, string containerName);

        // Database Operations (File Metadata)
        Task<UploadFile> GetFileMetadataAsync(string secureFileName);
        Task<UploadFile> SaveFileMetadataAsync(UploadFile uploadFile);
        Task<UploadFile> UpdateFileMetadataAsync(UploadFile uploadFile);
        Task<List<UploadFile>> GetFilesForBookingAsync(int bookingId);
        Task<List<UploadFile>> GetFilesForFacilityAsync(int facilityId);
        Task<bool> SoftDeleteFileMetadataAsync(string secureFileName);
    }
}
