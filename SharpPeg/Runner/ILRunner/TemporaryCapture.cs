using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    public unsafe class TemporaryCapture
    {
        public int CaptureKey;
        public char* StartIndex;
        public char* EndIndex;

        public TemporaryCapture(int captureKey, char* startIndex, char* endIndex)
        {
            CaptureKey = captureKey;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
