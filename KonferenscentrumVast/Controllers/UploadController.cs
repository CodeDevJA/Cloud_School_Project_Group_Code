using Microsoft.AspNetCore.Mvc;
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Services;
using KonferenscentrumVast.Validation;
using KonferenscentrumVast.Exceptions;

namespace KonferenscentrumVast.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;
        private readonly UploadValidation _validation;

        public UploadController(IUploadService uploadService, UploadValidation validation)
        {
            _uploadService = uploadService;
            _validation = validation;
        }

        /// <summary>
        /// Endpoint för file upload. Expects multipart/form-data med fält "file".
        /// (HTTP POST /api/upload)
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(50_000_000)] // exempel: 50 MB (kan justeras). (Request size limit)
        public async Task<IActionResult> Upload([FromForm] UploadRequest request)
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest(new { error = "No file provided" });

            // Validering (Validation layer)
            var validationResult = _validation.Validate(file);
            if (!validationResult.IsValid)
                return BadRequest(new { error = validationResult.ErrorMessage });

            try
            {
                var result = await _uploadService.UploadFileAsync(file, request.UploadedBy, HttpContext.RequestAborted);

                var response = new UploadResponse
                {
                    Id = result.Id,
                    BlobUrl = result.BlobUrl,
                    OriginalFileName = result.OriginalFileName,
                    Size = result.Size,
                    UploadedAt = result.UploadedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
            }
            catch (FileValidationException fvex)
            {
                return BadRequest(new { error = fvex.Message });
            }
            catch (StorageException sex)
            {
                // Loggat i service via ILogger
                return StatusCode(500, new { error = "Storage error", details = sex.Message });
            }
            catch (DatabaseException dbex)
            {
                return StatusCode(500, new { error = "Database error", details = dbex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unknown server error", details = ex.Message });
            }
        }

        // Liten GET för att hämta metadata (exempel)
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromServices] IUploadService uploadService, Guid id)
        {
            var entity = await uploadService.GetByIdAsync(id, CancellationToken.None);
            if (entity == null) return NotFound();
            var response = new UploadResponse
            {
                Id = entity.Id,
                BlobUrl = entity.BlobUrl,
                OriginalFileName = entity.OriginalFileName,
                Size = entity.Size,
                UploadedAt = entity.UploadedAt
            };
            return Ok(response);
        }
    }
}
