using System.Collections.Generic;

namespace SharpPeg.Common
{
    public class CompiledPeg
    {
        public int StartPatternIndex { get; }

        public IReadOnlyList<Method> Methods { get; }

        public Method StartPattern => Methods[StartPatternIndex];
        
        public CompiledPeg(IReadOnlyList<Method> methods, int startPatternIndex)
        {
            this.StartPatternIndex = startPatternIndex;
            Methods = methods;
        }
    }
}
