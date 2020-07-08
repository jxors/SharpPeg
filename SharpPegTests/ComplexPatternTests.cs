using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Common;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Optimizations;
using SharpPeg.Runner;
using SharpPeg.Runner.ILRunner;
using SharpPeg.Runner.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPegTests
{
    [TestClass]
    public class ComplexPatternTests
    {

        private static Operator RecursionHelperMethod(Pattern seed, Pattern grower)
        {
            const int VSTART = 0;
            const int VLASTOK = 1;

            const int LFAIL = 0;
            const int LLOOP = 1;
            const int LEND = 2;

            const int PSEED = 0;
            const int PGROW = 1;

            return new PrecompiledPattern(new Method(
                "RecursionHelper",
                new List<Instruction>()
                {
                    Instruction.StorePosition(VSTART),

                    // Seed
                    Instruction.Call(1, PSEED),
                    Instruction.Call(1, PGROW),

                    Instruction.Capture(VSTART, (ushort)2),

                    // Loop grower
                    Instruction.MarkLabel(LLOOP),
                    Instruction.StorePosition(VLASTOK),
                    Instruction.Call(0, PGROW),
                    Instruction.Capture(VSTART, (ushort)3),
                    Instruction.Jump(LLOOP),

                    Instruction.MarkLabel(LEND),
                    Instruction.RestorePosition(0, VLASTOK),
                    Instruction.Return(0),

                    // Fail boilerplate code
                    Instruction.MarkLabel(LFAIL),
                    Instruction.RestorePosition(0, VSTART),
                    Instruction.Return(1),
                },
                new List<CharRange>(),
                new List<LabelMap>
                {
                    new LabelMap(new[] { (1, (ushort)LEND) }),
                    new LabelMap(new[] { (1, (ushort)LFAIL) }),
                },
                2,
                3
            ), new List<Pattern>() { seed, grower });
        }

        [TestMethod]
        public void TestRecursion()
        {
            var add = new Pattern("Add");
            var whitespace = new Pattern("whitespace")
            {
                Data = new CaptureGroup(6, Operator.OneOrMore(new PrioritizedChoice(' ', '\t', '\r', '\n'))),
            };
            var optionalWhitespace = new Pattern("OptionalWhitespace")
            {
                Data = new CaptureGroup(4, Operator.Optional(whitespace)),
            };
            var intExp = new Pattern("IntExp")
            {
                Data = new CaptureGroup(2,
                    new Sequence(optionalWhitespace, Operator.Optional('-'), Operator.OneOrMore(new CharacterClass('0', '9')))
                )
            };
            var ifExp = new Pattern("If");
            var exp = new Pattern("Exp")
            {
                Data = new CaptureGroup(0, new PrioritizedChoice(add, intExp, ifExp)),
            };
            ifExp.Data = new CaptureGroup(3, new Sequence(optionalWhitespace, new CaptureGroup(5, CharacterClass.String("if")), exp, exp, exp));

            var grower = new Pattern("grower")
            {
                Data = new Sequence(new CharacterClass('+'), intExp),
            };

            add.Data = new CaptureGroup(0,
                RecursionHelperMethod(intExp, grower)
            );

            Match(exp, "if 1 0 1");
            Match(exp, "1+2+3");
            Match(exp, "1+2");
            Match(exp, "1+2+3+4+5+6+7+8+9");
        }

        private PatternCompiler[] compilers = new PatternCompiler[]
        {
            new PatternCompiler(new Compiler() { InlinePatterns = false }, new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EnableCaptureMemoization = true }),
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EnableCaptureMemoization = true }),
            new PatternCompiler(new Compiler(), new DefaultOptimizer
            {
                Optimizations = new SharpPeg.Optimizations.Default.OptimizationBase[0],
                RareOptimizations = new SharpPeg.Optimizations.Default.OptimizationBase[0],
            }, new InterpreterJitter()),
            PatternCompiler.Default,
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EnableCaptureMemoization = false }),
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new InterpreterJitter()),
        };

        private void Match(Operator p, string data)
        {
            var results = compilers.Select(compiler => {
                var runner = compiler.Compile(new Pattern() { Data = p });
                var list = new List<Capture>();
                runner.Run(data, list);

                return list;
            }).ToList();
        }
    }
}
