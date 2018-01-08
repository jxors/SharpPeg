using System;

namespace SharpPeg.Compilation
{
    public class CompilationException : Exception
    {
        public CompilationException()
        {
        }

        public CompilationException(string message) : base(message)
        {
        }

        public CompilationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}