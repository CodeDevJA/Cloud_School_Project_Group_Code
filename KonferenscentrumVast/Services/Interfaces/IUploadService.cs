using KonferenscentrumVast.Models;

namespace KonferenscentrumVast.Services
{
    // Service interface (Service layer)
    public interface IUploadService
    {
        Task<UploadFile> UploadFileAsync(IFormFile file, string? uploadedBy, CancellationToken cancellationToken);
        Task<UploadFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
