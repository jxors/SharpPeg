using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations
{
    public interface IOptimizer
    {
        CompiledPeg Optimize(CompiledPeg peg);
    }
}
