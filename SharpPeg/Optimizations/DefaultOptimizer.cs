using SharpPeg.Common;
using SharpPeg.Optimizations.Default;
using System.Linq;
using System.Diagnostics;

namespace SharpPeg.Optimizations
{
    public class DefaultOptimizer : IOptimizer
    {
        public OptimizationBase[] Optimizations { get; set; } = new OptimizationBase[] {
            new CharCheckOptimization(),
            new JumpOptimization(true),
            new RemoveUnusedVariablesOptimization(),
            new MergeVariableOptimization(),
            new ConsolidateBoundChecksOptimization(),
            new DelayAdvanceOptimization(),
            new RemoveUnneededRestorePositionOptimization(),
            new DelayStorePositionOptimization(),
            new RemoveUnneededVariableOperationsOptimization(),
            new RemoveUnusedAdvancesOptimization(),
            new DelayBoundsCheckOptimization(),
            new RemoveUnusedDiscardsOptimization(),
            new DeduplicationOptimization(),
            new RemoveUnusedStorePositionOptimization()
        };

        public OptimizationBase[] RareOptimizations { get; set; } = new OptimizationBase[] {
            new JumpOptimization(true),
            new RemoveUnneededChecksOptimization(true),
            new FastPathOptimization()
        };

        public DefaultOptimizer()
        {

        }

        public CompiledPeg Optimize(CompiledPeg peg)
        {
            return new CompiledPeg(peg.Methods.Select(method => OptimizeSingleMethod(method)).ToList(), peg.StartPatternIndex);
        }

        private Method OptimizeSingleMethod(Method m)
        {
            var cleanup = new OptimizationBase[] {
                new QuickCleanupOptimization(),
                new FastJumpCleanupOptimization(),
            };

            var context = new OptimizationContext(m);

            for (var i = 0; i < 256; i++)
            {
                Debug.WriteLine($"Optimization iteration {i}");

                var changed = Optimize(context, cleanup);
                foreach (var optimizer in Optimizations)
                {
                    if (optimizer.Optimize(context))
                    {
                        if (Optimize(context, cleanup))
                        {
                            // TODO: Make sure cleanup needs only 1 iteration
                            while (Optimize(context, cleanup))
                            { }
                        }

                        changed = true;
                    }
                }

                if (!changed)
                {
                    foreach (var optimizer in RareOptimizations)
                    {
                        changed |= optimizer.Optimize(context);
                    }

                    if (!changed)
                    {
                        break;
                    }
                }
            }

            foreach (var c in cleanup)
            {
                c.Optimize(context);
            }

            return new Method(m.Name, context.Instructions, m.CharacterRanges, context.VariableAllocator, context.LabelAllocator);
        }

        private static bool Optimize(OptimizationContext context, OptimizationBase[] items)
        {
            var changed = false;
            foreach (var c in items)
            {
                changed |= c.Optimize(context);
            }

            return changed;
        }
    }
}
