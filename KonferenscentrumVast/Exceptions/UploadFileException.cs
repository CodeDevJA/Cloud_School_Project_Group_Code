using System;

namespace KonferenscentrumVast.Exceptions
{
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

        // FIXED: Correct parameter order
        public UploadFileException(string message, string userFriendlyMessage, Exception innerException, string errorCode = "FILE_UPLOAD_ERROR")
            : base(message, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode;
        }
    }

    public class FileValidationException : UploadFileException
    {
        public FileValidationException(string validationError)
            : base($"File validation failed: {validationError}",
                  validationError,
                  "FILE_VALIDATION_ERROR")
        {
        }
    }

    public class FileSizeException : UploadFileException
    {
        public FileSizeException(long actualSize, long maxSize)
            : base($"File size {actualSize} bytes exceeds maximum {maxSize} bytes",
                  "The file is too large. Please choose a smaller file.",
                  "FILE_SIZE_EXCEEDED")
        {
        }
    }

    public class FileTypeException : UploadFileException
    {
        public FileTypeException(string fileExtension)
            : base($"File type '{fileExtension}' is not allowed",
                  "This file type is not supported. Please use a different file format.",
                  "FILE_TYPE_NOT_ALLOWED")
        {
        }
    }

    // FIXED: Remove the duplicate FileNotFoundException to avoid ambiguity
    public class StorageException : UploadFileException
    {
        public StorageException(string operation, Exception innerException)
            : base($"Storage operation '{operation}' failed",
                  "Unable to process the file at this time. Please try again.",
                  innerException,
                  "STORAGE_OPERATION_FAILED")
        {
        }
    }

    public class SecurityException : UploadFileException
    {
        public SecurityException(string securityIssue)
            : base($"Security check failed: {securityIssue}",
                  "The file could not be processed for security reasons.",
                  "SECURITY_CHECK_FAILED")
        {
        }
    }

    // REMOVED: FileNotFoundException to avoid conflict with System.IO.FileNotFoundException
}
