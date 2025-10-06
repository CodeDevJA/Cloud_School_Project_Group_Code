using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KonferenscentrumVast.Data;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Exceptions;

namespace KonferenscentrumVast.Repositories
{
    // Implementation av repository (EF Core mot PostgreSQL)
    public class UploadRepository : IUploadRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UploadRepository> _logger;

        public UploadRepository(ApplicationDbContext dbContext, ILogger<UploadRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SaveAsync(UploadFile entity, CancellationToken cancellationToken)
        {
            try
            {
                _dbContext.UploadFiles.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException dbex)
            {
                _logger.LogError(dbex, "Database update error while saving upload metadata");
                throw new DatabaseException("Failed to save upload metadata", dbex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected database error");
                throw new DatabaseException("Unexpected database error", ex);
            }
        }

        public async Task<UploadFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.UploadFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching upload metadata");
                throw new DatabaseException("Error fetching upload metadata", ex);
            }
        }
    }
}
