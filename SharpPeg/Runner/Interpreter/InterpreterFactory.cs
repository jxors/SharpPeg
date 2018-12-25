using System.Collections.Generic;
using SharpPeg.Common;

namespace SharpPeg.Runner.Interpreter
{
    internal class InterpreterFactory : IRunnerFactory
    {
        private int startPatternIndex;
        private IReadOnlyList<Method> methods;
        private int[][] labelPositions;

        public InterpreterFactory(int startPatternIndex, IReadOnlyList<Method> methods, int[][] labelPositions)
        {
            this.startPatternIndex = startPatternIndex;
            this.methods = methods;
            this.labelPositions = labelPositions;
        }

        public IRunner New()
        {
            return new InterpreterRunner(startPatternIndex, methods, labelPositions);
        }
    }
}