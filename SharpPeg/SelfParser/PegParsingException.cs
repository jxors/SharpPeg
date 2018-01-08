using System;

namespace SharpPeg.SelfParser
{
    public class PegParsingException : Exception
    {
        public PegParsingException()
        {
        }

        public PegParsingException(string message) : base(message)
        {
        }

        public PegParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}