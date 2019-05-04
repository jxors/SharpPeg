using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnusedVariablesOptimization : OptimizationBase
    {
        class VariableOffset
        {
            private Dictionary<int, int> offsets = new Dictionary<int, int>();

            public VariableOffset()
            { }

            private VariableOffset(Dictionary<int, int> offsets)
            {
                this.offsets = offsets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            public void DoStore(int variable)
            {
                offsets[variable] = 0;
            }

            public void UnionWith(VariableOffset other, int offset = 0)
            {
                foreach(var kvp in other.offsets)
                {
                    if(kvp.Value == -1)
                    {
                        offsets[kvp.Key] = -1;
                    } else if (offsets.TryGetValue(kvp.Key, out var value))
                    {
                        if (value == -1 || value != kvp.Value + offset)
                        {
                            offsets[kvp.Key] = -1;
                        }
                    }
                    else
                    {
                        offsets[kvp.Key] = kvp.Value + offset;
                    }
                }
            }

            public bool Equals(VariableOffset other)
            {
                foreach (var kvp in other.offsets)
                {
                    if (offsets.TryGetValue(kvp.Key, out var value))
                    {
                        if (value != kvp.Value)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            public int Get(int variable)
            {
                if (offsets.TryGetValue(variable, out var value))
                {
                    return value;
                }

                return -1;
            }

            public VariableOffset Clone()
            {
                return new VariableOffset(offsets);
            }

            public override string ToString()
            {
                return string.Join(", ", offsets.Select(kvp => $"{kvp.Key} => {kvp.Value}"));
            }

            internal void Invalidate()
            {
                foreach(var key in offsets.Keys.ToArray())
                {
                    offsets[key] = -1;
                }
            }
        }

        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            var offsetInfo = new VariableOffset[context.Count];

            for(var i = 0; i < offsetInfo.Length; i++)
            {
                offsetInfo[i] = new VariableOffset();
            }
            
            BuildOffsetInfo(context, offsetInfo);

            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instruction = context[i];
                switch(instruction.Type)
                {
                    case InstructionType.RestorePosition:
                        var offset = offsetInfo[i].Get(instruction.Data1);
                        if (offset == 0)
                        {
                            changed = true;
                            context.RemoveAt(i);
                        } else if (offset > 0)
                        {
                            changed = true;
                            context[i] = Instruction.Advance((short)-offset);
                        }
                        break;
                }
            }

            return changed;
        }

        private void BuildOffsetInfo(OptimizationContext context, VariableOffset[] offsetInfo)
        {
            VariableOffset[] oldOffsetInfo;
            do
            {
                var labelPositions = CalculateLabelPositions(context);
                oldOffsetInfo = offsetInfo.Select(item => item.Clone()).ToArray();
                for (var i = 0; i < context.Count - 1; i++)
                {
                    var instruction = context[i];
                    var currentInfo = offsetInfo[i];
                    switch (instruction.Type)
                    {
                        case InstructionType.BoundsCheck:
                        case InstructionType.Char:
                            offsetInfo[labelPositions[instruction.Label]].UnionWith(currentInfo);
                            break;
                        case InstructionType.StorePosition:
                            currentInfo.DoStore(instruction.Data1);
                            break;
                        case InstructionType.Jump:
                            offsetInfo[labelPositions[instruction.Label]].UnionWith(currentInfo);
                            break;
                        case InstructionType.Call:
                            currentInfo.Invalidate();
                            foreach (var (_, jumpTarget) in context.FailureLabelMap[instruction.Data2].Mapping)
                            {
                                offsetInfo[labelPositions[jumpTarget]].UnionWith(currentInfo);
                            }
                            break;
                    }

                    if (instruction.Type != InstructionType.Jump)
                    {
                        switch (context[i + 1].Type)
                        {
                            case InstructionType.Advance:
                                offsetInfo[i + 1].UnionWith(currentInfo, (int)context[i + 1].Offset);
                                break;
                            default:
                                offsetInfo[i + 1].UnionWith(currentInfo);
                                break;
                        }
                    }
                }
                
            } while (!OffsetsAreEqual(offsetInfo, oldOffsetInfo));
        }

        private bool OffsetsAreEqual(VariableOffset[] offsetInfo, VariableOffset[] oldOffsetInfo)
        {
            for(var i = 0; i < offsetInfo.Length;i++)
            {
                if(!offsetInfo[i].Equals(oldOffsetInfo[i]) || !oldOffsetInfo[i].Equals(offsetInfo[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
