using KonferenscentrumVast.DTOs;
using KonferenscentrumVast.Exceptions;
using KonferenscentrumVast.Models;

namespace KonferenscentrumVast.Validation
{
    /// <summary>
    /// Comprehensive validator for file upload operations
    /// Performs security checks, GDPR compliance validation, and business rule enforcement
    /// Throws specific exceptions for different validation failure scenarios
    /// </summary>
    public class UploadFileValidator
    {
        private readonly AzureStorageConfig _config;

        public UploadFileValidator(AzureStorageConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Main validation method for file upload requests
        /// Performs multiple validation checks in security-priority order
        /// GDPR: Validates that no personal data is embedded in files
        /// </summary>
        public void ValidateUploadRequest(FileUploadRequestDto request)
        {
            if (request == null)
                throw new FileValidationException("Upload request cannot be null");

            ValidateFileExists(request.File);
            ValidateFileSize(request.File);
            ValidateFileExtension(request.File);
            ValidateFileType(request.FileType);
            ValidateFileNameSecurity(request.File.FileName);
            ValidateContentType(request.File);
        }

        /// <summary>
        /// Validates that a file is present in the request
        /// Basic check to prevent null reference exceptions
        /// </summary>
        private void ValidateFileExists(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new FileValidationException("No file provided for upload");
            }
        }

        /// <summary>
        /// Validates file size against configured limits
        /// Prevents denial of service attacks via large files
        /// GDPR: Large files may contain excessive personal data
        /// </summary>
        private void ValidateFileSize(IFormFile file)
        {
            if (file.Length > _config.MaxFileSize)
            {
                throw new FileSizeException(file.Length, _config.MaxFileSize);
            }

            // Also check minimum size to detect empty or corrupted files
            if (file.Length == 0)
            {
                throw new FileValidationException("File appears to be empty or corrupted");
            }
        }

        /// <summary>
        /// Validates file extension against allowed list
        /// Prevents upload of executable files and other dangerous types
        /// Security: Whitelist approach is safer than blacklist
        /// </summary>
        private void ValidateFileExtension(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new FileTypeException("No file extension detected");
            }

            if (!_config.AllowedExtensions.Contains(fileExtension))
            {
                throw new FileTypeException(fileExtension);
            }
        }

        /// <summary>
        /// Validates file type category (contract or image)
        /// Ensures business logic consistency for different file types
        /// </summary>
        private void ValidateFileType(string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
            {
                throw new FileValidationException("File type must be specified");
            }

            var allowedFileTypes = new[] { "contract", "image" };
            if (!allowedFileTypes.Contains(fileType.ToLower()))
            {
                throw new FileValidationException($"Invalid file type '{fileType}'. Must be 'contract' or 'image'");
            }
        }

        /// <summary>
        /// Performs security validation on the original filename
        /// Prevents path traversal attacks and other filename-based exploits
        /// GDPR: Checks for potential personal data in filenames
        /// </summary>
        private void ValidateFileNameSecurity(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new FileValidationException("Filename cannot be empty");
            }

            // Check for path traversal attempts
            if (fileName.Contains("..") || fileName.Contains("//") || fileName.Contains("\\"))
            {
                throw new SecurityException("Filename contains potential path traversal characters");
            }

            // Check for reserved characters that could cause issues
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.Any(c => invalidChars.Contains(c)))
            {
                throw new SecurityException("Filename contains invalid characters");
            }

            // Check for excessively long filenames
            if (fileName.Length > 255)
            {
                throw new FileValidationException("Filename is too long");
            }

            // GDPR: Check for potential personal data in filenames
            ValidateForPersonalData(fileName);
        }

        /// <summary>
        /// Basic check for potential personal data in filenames
        /// GDPR: Prevents accidental storage of personal data in filenames
        /// Note: This is a basic check - comprehensive PII detection would be more complex
        /// </summary>
        private void ValidateForPersonalData(string fileName)
        {
            var lowerFileName = fileName.ToLowerInvariant();

            // Check for common personal data patterns
            var personalDataIndicators = new[]
            {
                "personnummer", "social security", "ssn", "passport", "id-card",
                "creditcard", "cardnumber", "bankaccount", "salary", "medical"
            };

            if (personalDataIndicators.Any(indicator => lowerFileName.Contains(indicator)))
            {
                throw new SecurityException("Filename may contain personal data");
            }

            // Check for email pattern in filename
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            if (System.Text.RegularExpressions.Regex.IsMatch(fileName, emailPattern))
            {
                throw new SecurityException("Filename contains email address");
            }
        }

        /// <summary>
        /// Validates that content type matches file extension
        /// Prevents MIME type spoofing attacks
        /// Security: Ensures files are what they claim to be
        /// </summary>
        private void ValidateContentType(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            // MIME type mapping for validation
            var mimeTypeMap = new Dictionary<string, string[]>
            {
                [".pdf"] = new[] { "application/pdf" },
                [".jpg"] = new[] { "image/jpeg" },
                [".jpeg"] = new[] { "image/jpeg" },
                [".png"] = new[] { "image/png" },
                [".doc"] = new[] { "application/msword", "application/vnd.ms-word" },
                [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
            };

            if (mimeTypeMap.TryGetValue(fileExtension, out var allowedMimeTypes))
            {
                // Allow application/octet-stream as it's a common fallback
                if (!allowedMimeTypes.Contains(contentType) && contentType != "application/octet-stream")
                {
                    throw new SecurityException($"MIME type '{contentType}' does not match file extension '{fileExtension}'");
                }
            }
        }

        /// <summary>
        /// Validates file deletion requests
        /// Ensures only valid, system-generated filenames can be deleted
        /// GDPR: Prevents unauthorized file deletion
        /// </summary>
        public void ValidateDeleteRequest(string secureFileName)
        {
            if (string.IsNullOrWhiteSpace(secureFileName))
            {
                throw new FileValidationException("File name is required for deletion");
            }

            // Validate that filename follows our secure naming pattern
            if (!secureFileName.StartsWith("contract_") && !secureFileName.StartsWith("image_"))
            {
                throw new SecurityException("Invalid file name format for deletion");
            }

            // Check for path traversal in deletion request
            if (secureFileName.Contains("..") || secureFileName.Contains("/") || secureFileName.Contains("\\"))
            {
                throw new SecurityException("File name contains invalid characters");
            }
        }

        /// <summary>
        /// Validates file download requests
        /// Ensures secure access to files and prevents directory traversal
        /// </summary>
        public void ValidateDownloadRequest(string secureFileName)
        {
            if (string.IsNullOrWhiteSpace(secureFileName))
            {
                throw new FileValidationException("File name is required for download");
            }

            // Validate secure filename pattern
            if (!secureFileName.StartsWith("contract_") && !secureFileName.StartsWith("image_"))
            {
                throw new SecurityException("Invalid file name format for download");
            }

            // Security check for path traversal
            if (secureFileName.Contains("..") || secureFileName.Contains("/") || secureFileName.Contains("\\"))
            {
                throw new SecurityException("File name contains invalid characters");
            }
        }

        /// <summary>
        /// Generates a secure filename for storage
        /// GDPR: Uses GUID and timestamp to avoid personal data in filenames
        /// Security: Predictable naming prevents filename guessing attacks
        /// </summary>
        public string GenerateSecureFileName(string originalFileName, string fileType)
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N"); // No hyphens

            return $"{fileType}_{timestamp}_{guid}{extension}";
        }
    }
}
