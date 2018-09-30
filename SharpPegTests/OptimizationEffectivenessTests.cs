using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg.Common;
using SharpPeg.Compilation;
using SharpPeg.Optimizations.Default;
using SharpPeg.Operators;
using SharpPeg.Runner;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpPeg.Optimizations;

namespace SharpPegTests
{
    [TestClass]
    public class OptimizationEffectivenessTests
    {
        private class RunnerMock : IRunner
        {
            public List<Method> Methods;

            public RunnerMock(List<Method> methods)
            {
                this.Methods = methods;
            }

            public string ExplainResult(RunResult result, string inputData) => "";

            public IEnumerable<string> GetPatternsFinishedAt(int index)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetPatternsTriedAt(int startIndex)
            {
                throw new NotImplementedException();
            }

            public RunResult Run(string stringData, List<Capture> captureOutput = null) => default(RunResult);

            public RunResult Run(char[] data, int index, int length, List<Capture> captureOutput = null) => default(RunResult);
        }

        [TestMethod]
        public void CharCheckOptimization() => OptimizationEffectiveness<CharCheckOptimization>();

        [TestMethod]
        public void JumpOptimization() => OptimizationEffectiveness<JumpOptimization>();

        [TestMethod]
        public void RemoveUnusedVariablesOptimization() => OptimizationEffectiveness<RemoveUnusedVariablesOptimization>();

        [TestMethod]
        public void ConsolidateBoundChecksOptimization() => OptimizationEffectiveness<ConsolidateBoundChecksOptimization>();

        [TestMethod]
        public void ConsolidateAdvanceOptimization() => OptimizationEffectiveness<DelayAdvanceOptimization>();

        [TestMethod]
        public void RemoveUnneededRestorePositionOptimization() => OptimizationEffectiveness<RemoveUnneededRestorePositionOptimization>();

        [TestMethod]
        public void RemoveUnusedStorePositionOptimization() => OptimizationEffectiveness<RemoveUnusedStorePositionOptimization>();

        [TestMethod]
        public void DelayStorePositionOptimization() => OptimizationEffectiveness<DelayStorePositionOptimization>();

        [TestMethod]
        public void RemoveUnusedAdvancesOptimization() => OptimizationEffectiveness<RemoveUnusedAdvancesOptimization>();

        [TestMethod]
        public void DelayBoundsCheckOptimization() => OptimizationEffectiveness<DelayBoundsCheckOptimization>();

        [TestMethod]
        public void DeduplicationOptimization() => OptimizationEffectiveness<DeduplicationOptimization>();

        [TestMethod]
        public void FastPathOptimization() => OptimizationEffectiveness<FastPathOptimization>();

        [TestMethod]
        public void RemoveUnneededVariableOperationsOptimization() => OptimizationEffectiveness<RemoveUnneededVariableOperationsOptimization>();

        [TestMethod]
        public void RemoveUnneededChecksOptimization() => OptimizationEffectiveness<RemoveUnneededChecksOptimization>();

        [TestMethod]
        public void MergeVariableOptimization() => OptimizationEffectiveness<MergeVariableOptimization>();

        //[TestMethod]
        //public void RemoveUnneededStorePositionOptimization() => OptimizationEffectiveness<RemoveUnusedStorePositionOptimization>();

        private void OptimizationEffectiveness<T>()
        {
            var optimizations = new OptimizationBase[] {
                new CharCheckOptimization(),
                new JumpOptimization(true),
                new RemoveUnusedVariablesOptimization(),
                new MergeVariableOptimization(),
                new ConsolidateBoundChecksOptimization(),
                new DelayAdvanceOptimization(),
                new RemoveUnneededRestorePositionOptimization(),
                new RemoveUnusedStorePositionOptimization(),
                new DelayStorePositionOptimization(),
                new RemoveUnusedAdvancesOptimization(),
                new DelayBoundsCheckOptimization(),
                new DeduplicationOptimization(),
                new RemoveUnneededChecksOptimization(true),
                new RemoveUnneededVariableOperationsOptimization(),
                new FastPathOptimization(),
            };

            TestEffectiveness(typeof(T).Name, new DefaultOptimizer()
            {
                Optimizations = optimizations.Where(o => !(o is T)).ToArray(),
                RareOptimizations = new OptimizationBase[0]
            }, new DefaultOptimizer()
            {
                Optimizations = optimizations,
                RareOptimizations = new OptimizationBase[0]
            });
        }

        private void TestEffectiveness(string name, IOptimizer without, IOptimizer with)
        {
            var noEnters = new Sequence(new Not(new CharacterClass('\n', '\r')), new Any());
            var ops = new Operator[]
            {
                new Sequence(new CharacterClass('a', 'z'), new CharacterClass('b')),
                new ZeroOrMore(new CaptureGroup(0, new PrioritizedChoice("abcdef", new CaptureGroup(0, "xxcde")))),
                Operator.EndingWithGreedy(new CharacterClass('a', 'z'), "ing"),
                new ZeroOrMore(new CaptureGroup(0, new PrioritizedChoice(
                    new Sequence(noEnters, noEnters, new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn")),
                    new Sequence(noEnters, new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn")),
                    new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn")
                ))),
                new CaptureGroup(0, new ZeroOrMore(new CaptureGroup(0, new PrioritizedChoice(
                    new Sequence(noEnters, noEnters, new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn")),
                    new CaptureGroup(2, new Sequence(noEnters, new CaptureGroup(1, new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn")))),
                    new CaptureGroup(1, new PrioritizedChoice("Tom", "Sawyer", "Huckleberry", "Finn"))
                )))),
            };

            foreach (var op in ops)
            {
                if(OptimizationMakesDifferenceInSinglePattern(without, with, op))
                {
                    Console.WriteLine($"{name} makes a difference for {op}");
                    return;
                }
            }

            Assert.Fail($"Optimization {name} has no effect");
        }

        private static bool OptimizationMakesDifferenceInSinglePattern(IOptimizer without, IOptimizer with, Operator op)
        {
            var p = new Pattern { Data = op };
            var unoptimized = new Compiler().Compile(p);
            var withoutOptimizationRunner = without.Optimize(unoptimized);
            var withOptimizationRunner = with.Optimize(unoptimized);

            return !AreEqual(withoutOptimizationRunner.Methods[0].Instructions, withOptimizationRunner.Methods[0].Instructions);
        }

        private static bool AreEqual(IReadOnlyList<Instruction> instructionsA, IReadOnlyList<Instruction> instructionsB)
        {
            var labelMappings = new int?[1000];

            if(instructionsA.Count != instructionsB.Count)
            {
                return false;
            }

            for(var i = 0; i < instructionsA.Count; i++)
            {
                var left = instructionsA[i];
                var right = instructionsB[i];
                switch (left.Type)
                {
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                    case InstructionType.Jump:
                    case InstructionType.Call:
                    case InstructionType.MarkLabel:
                        if(!right.HasLabel)
                        {
                            return false;
                        }

                        if(labelMappings[left.Label] == null)
                        {
                            labelMappings[left.Label] = right.Label;
                        }

                        if(labelMappings[left.Label] != right.Label
                            || left.Type != right.Type
                            || left.Data1 != right.Data1
                            || left.Data2 != right.Data2)
                        {
                            return false;
                        }
                        break;
                    default:
                        if(left != right)
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }
    }
}