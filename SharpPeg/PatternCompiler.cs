using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Optimizations;
using SharpPeg.Runner;
using SharpPeg.Runner.ILRunner;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg
{
    public class PatternCompiler
    {
        public static PatternCompiler Default = new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter());

        public ICompiler Compiler { get; }
        public IOptimizer Optimizer { get; }

        public IJitter Jitter { get; }
        
        public PatternCompiler(ICompiler compiler, IOptimizer optimizer, IJitter jitter)
        {
            Compiler = compiler;
            Optimizer = optimizer;
            Jitter = jitter;
        }

        public IRunner Compile(Pattern pattern)
        {
            var compiledPeg = Compiler.Compile(pattern);
            var optimizedPeg = Optimizer?.Optimize(compiledPeg) ?? compiledPeg;
            var runner = Jitter.Compile(optimizedPeg);

            return runner;
        }

        public IRunnerFactory CompileAsFactory(Pattern pattern)
        {
            var compiledPeg = Compiler.Compile(pattern);
            var optimizedPeg = Optimizer?.Optimize(compiledPeg) ?? compiledPeg;
            var runner = Jitter.CompileAsFactory(optimizedPeg);

            return runner;
        }
    }
}
