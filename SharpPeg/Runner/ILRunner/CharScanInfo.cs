using System.Collections.Generic;

namespace SharpPeg.Runner.ILRunner
{
    public class CharScanInfo
    {
        public IReadOnlyList<char> SearchFor { get; }
        public int StartOffset { get; }
        public int Bounds { get; }

        public CharScanInfo(int bounds, int startOffset, IReadOnlyList<char> searchFor)
        {
            this.Bounds = bounds;
            this.StartOffset = startOffset;
            this.SearchFor = searchFor;
        }
    }
}