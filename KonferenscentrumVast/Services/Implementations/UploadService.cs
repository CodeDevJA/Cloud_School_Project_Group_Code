using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Repositories;
using KonferenscentrumVast.Exceptions;

namespace KonferenscentrumVast.Services
{
    // Implementation av service (affärslogik) (Service)
    public class UploadService : IUploadService
    {
        private readonly IUploadRepository _repository;
        private readonly BlobServiceClient _blobServiceClient; // Azure Blob Storage client
        private readonly ILogger<UploadService> _logger;
        private readonly string _containerName = "uploads"; // rekommendation: konfigurera via settings

        public UploadService(
            IUploadRepository repository,
            BlobServiceClient blobServiceClient,
            ILogger<UploadService> logger)
        {
            _repository = repository;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public async Task<UploadFile> UploadFileAsync(IFormFile file, string? uploadedBy, CancellationToken cancellationToken)
        {
            if (file == null) throw new FileValidationException("No file provided");

            // Generera unik filnamn (StoredFileName)
            var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            try
            {
                // Get container client
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                var blobClient = containerClient.GetBlobClient(storedFileName);

                // Optionellt: använd BlockBlobClient för stora filer
                using (var stream = file.OpenReadStream())
                {
                    // Upload to Blob (Azure Blob Storage)
                    await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
                }

                var blobUrl = blobClient.Uri.ToString();

                // Skapa entity för DB
                var entity = new UploadFile
                {
                    Id = Guid.NewGuid(),
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    BlobUrl = blobUrl,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTimeOffset.UtcNow
                };

                // Spara metadata i DB via repository
                await _repository.SaveAsync(entity, cancellationToken);

                _logger.LogInformation("File uploaded successfully. Id: {FileId}, BlobUrl: {BlobUrl}", entity.Id, entity.BlobUrl);

                return entity;
            }
            catch (FileValidationException)
            {
                throw; // bubbla upp valideringsfel
            }
            catch (Azure.RequestFailedException rfe)
            {
                _logger.LogError(rfe, "Azure Blob upload failed for file {FileName}", file.FileName);
                throw new StorageException("Failed to upload file to blob storage", rfe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file upload for {FileName}", file.FileName);
                throw new StorageException("Unexpected error during storage operation", ex);
            }
        }

        public async Task<UploadFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(id, cancellationToken);
        }
    }
}
