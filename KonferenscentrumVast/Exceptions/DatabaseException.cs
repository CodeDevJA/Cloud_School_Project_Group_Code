using System;

namespace KonferenscentrumVast.Exceptions
{
    // Custom exception för databasfel (Database exception)
    public class DatabaseException : Exception
    {
        public DatabaseException() { }
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
    }
}
