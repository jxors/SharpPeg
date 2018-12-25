using System.Collections.Generic;
using SharpPeg.Operators;
using SharpPeg.Common;

namespace SharpPeg.Runner
{
    public interface IJitter
    {
        IRunner Compile(CompiledPeg compiledPeg);

        IRunnerFactory CompileAsFactory(CompiledPeg peg);
    }
}