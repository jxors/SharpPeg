using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;

namespace SharpPeg.Optimizations.Default.PushDown
{
    public abstract class PushDownOptimization : OptimizationBase
    {
        private readonly InstructionType type;

        protected PushDownOptimization(InstructionType type)
        {
            this.type = type;
        }

        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = 0; i < context.Count; i++)
            {
                if (context[i].Type == type)
                {
                    if(!IsChainBreaker(context[i + 1], context[i], new BacktracerView(context.Backtracer, i - 1, false)) && InnerOptimize(context, context[i], context.Backtracer, i))
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }

        protected abstract bool InnerOptimize(OptimizationContext context, Instruction matchedInstruction, Backtracer backtracer, int i);

        protected abstract bool IsChainBreaker(Instruction instruction, Instruction source, BacktracerView btv);
    }
}
