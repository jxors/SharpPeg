using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnneededVariableOperationsOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = 0; i < context.Count; i++)
            {
                var storeInstruction = context[i];
                if (storeInstruction.Type == InstructionType.StorePosition)
                {
                    if (!IsNeeded(0, storeInstruction, context, i + 1))
                    {
                        changed = true;
                        context.RemoveAt(i);
                    }
                    else if (context[i + 1].Type == InstructionType.Jump)
                    {
                        var labelPos = context.GetLabelPosition(context[i + 1].Label);
                        if (context[labelPos + 1].Type == InstructionType.RestorePosition && context[labelPos + 1].Data1 == storeInstruction.Data1)
                        {
                            changed = true;
                            ushort newLabel;
                            if (context[labelPos + 2].Type == InstructionType.DiscardCaptures)
                            {
                                // Create stub that stores position
                                newLabel = AddStubByPosition(context, labelPos + 3, Instruction.StorePosition(storeInstruction.Data1));
                                if (labelPos + 3 < i)
                                {
                                    i += 4;
                                }
                            }
                            else
                            {
                                newLabel = AddStubByPosition(context, labelPos + 2, Instruction.StorePosition(storeInstruction.Data1));
                                if (labelPos + 2 < i)
                                {
                                    i += 4;
                                }
                            }

                            context[i] = Instruction.Jump(newLabel);
                        }
                    }
                    else if (context[i + 1].Type == InstructionType.MarkLabel
                       && context[i + 2].Type == InstructionType.RestorePosition
                       && context[i + 2].Data1 == storeInstruction.Data1)
                    {
                        changed = true;
                        var offset = 3;
                        if (context[i + 3].Type == InstructionType.DiscardCaptures)
                        {
                            offset = 4;
                        }

                        var newLabel = context.LabelAllocator++;
                        context[i] = Instruction.Jump(newLabel);

                        context.Insert(i + offset, Instruction.MarkLabel(newLabel));
                        context.Insert(i + offset + 1, Instruction.StorePosition(storeInstruction.Data1));
                    }
                }
            }

            return changed;
        }

        private bool IsNeeded(int depth, Instruction storeInstruction, OptimizationContext context, int position)
        {
            if (depth > 8)
            {
                return true;
            }

            while (true)
            {
                var instruction = context[position];
                switch (instruction.Type)
                {
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                        return IsNeeded(depth + 1, storeInstruction, context, position + 1) || IsNeeded(depth + 1, storeInstruction, context, context.GetLabelPosition(instruction.Label));
                    case InstructionType.StorePosition:
                        if (instruction.Data1 == storeInstruction.Data1)
                        {
                            return false;
                        }

                        position++;
                        break;
                    case InstructionType.Return:
                        return false;
                    case InstructionType.Jump:
                        position = context.GetLabelPosition(instruction.Label);
                        break;
                    case InstructionType.MarkLabel:
                    case InstructionType.Advance:
                        position++;
                        break;
                    default:
                        return true;
                }
            }
        }
    }
}
