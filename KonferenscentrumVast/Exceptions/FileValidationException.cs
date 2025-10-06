using System;

namespace KonferenscentrumVast.Exceptions
{
    // Custom exception för valideringsfel (Validation exception)
    public class FileValidationException : Exception
    {
        public FileValidationException() { }
        public FileValidationException(string message) : base(message) { }
        public FileValidationException(string message, Exception inner) : base(message, inner) { }
    }
}
