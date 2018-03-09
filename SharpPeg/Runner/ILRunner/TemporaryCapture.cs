using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    public unsafe class TemporaryCapture
    {
        public int CaptureKey;
        public int OpenIndex;
        public char* StartIndex;
        public char* EndIndex;

        public TemporaryCapture(int captureKey, int openIndex, char* startIndex, char* endIndex)
        {
            CaptureKey = captureKey;
            OpenIndex = openIndex;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
