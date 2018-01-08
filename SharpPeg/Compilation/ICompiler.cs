using SharpPeg.Common;
using SharpPeg.Operators;

namespace SharpPeg.Compilation
{
    public interface ICompiler
    {
        CompiledPeg Compile(Pattern pattern);
    }
}
