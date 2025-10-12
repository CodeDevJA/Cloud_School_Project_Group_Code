using System;

namespace KonferenscentrumVast.Exceptions
{
    /// <summary>
    /// Base exception for all file upload related errors
    /// Provides consistent error handling across the application
    /// </summary>
    public class UploadFileException : Exception
    {
        public string UserFriendlyMessage { get; }
        public string ErrorCode { get; }

        public UploadFileException(string message, string userFriendlyMessage, string errorCode = "FILE_UPLOAD_ERROR") 
            : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode;
        }

        public UploadFileException(string message, string userFriendlyMessage, Exception innerException, string errorCode = "FILE_UPLOAD_ERROR") 
            : base(message, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Thrown when file validation fails (size, type, security checks)
    /// GDPR: Error messages don't expose sensitive system information
    /// </summary>
    public class FileValidationException : UploadFileException
    {
        public FileValidationException(string validationError) 
            : base($"File validation failed: {validationError}", 
                  validationError, // User sees the same message for simplicity
                  "FILE_VALIDATION_ERROR")
        {
        }
    }

    /// <summary>
    /// Thrown when file size exceeds allowed limit
    /// GDPR: Message doesn't reveal system limits to potential attackers
    /// </summary>
    public class FileSizeException : UploadFileException
    {
        public FileSizeException(long actualSize, long maxSize) 
            : base($"File size {actualSize} bytes exceeds maximum {maxSize} bytes", 
                  "The file is too large. Please choose a smaller file.",
                  "FILE_SIZE_EXCEEDED")
        {
        }
    }

    /// <summary>
    /// Thrown when file type is not allowed
    /// GDPR: Message doesn't reveal allowed file types to potential attackers
    /// </summary>
    public class FileTypeException : UploadFileException
    {
        public FileTypeException(string fileExtension) 
            : base($"File type '{fileExtension}' is not allowed", 
                  "This file type is not supported. Please use a different file format.",
                  "FILE_TYPE_NOT_ALLOWED")
        {
        }
    }

    /// <summary>
    /// Thrown when Azure Blob Storage operations fail
    /// GDPR: Internal error details are logged, user gets generic message
    /// </summary>
    public class StorageException : UploadFileException
    {
        public StorageException(string operation, Exception innerException) 
            : base($"Storage operation '{operation}' failed", 
                  "Unable to process the file at this time. Please try again.",
                  "STORAGE_OPERATION_FAILED", 
                  innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when file contains potential security risks
    /// GDPR: Generic message to avoid helping malicious users
    /// </summary>
    public class SecurityException : UploadFileException
    {
        public SecurityException(string securityIssue) 
            : base($"Security check failed: {securityIssue}", 
                  "The file could not be processed for security reasons.",
                  "SECURITY_CHECK_FAILED")
        {
        }
    }

    /// <summary>
    /// Thrown when file is not found during operations
    /// GDPR: Message doesn't reveal file existence information
    /// </summary>
    public class FileNotFoundException : UploadFileException
    {
        public FileNotFoundException(string fileName) 
            : base($"File '{fileName}' not found", 
                  "The requested file could not be found.",
                  "FILE_NOT_FOUND")
        {
        }
    }
}
