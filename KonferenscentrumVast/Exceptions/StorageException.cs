using System;

namespace KonferenscentrumVast.Exceptions
{
    // Custom exception för lagringsfel (Storage exception)
    public class StorageException : Exception
    {
        public StorageException() { }
        public StorageException(string message) : base(message) { }
        public StorageException(string message, Exception inner) : base(message, inner) { }
    }
}
