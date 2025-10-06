using System;
using System.Threading;
using System.Threading.Tasks;
using KonferenscentrumVast.Models;

namespace KonferenscentrumVast.Repositories
{
    // Repository interface (Repository pattern)
    public interface IUploadRepository
    {
        Task SaveAsync(UploadFile entity, CancellationToken cancellationToken);
        Task<UploadFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
