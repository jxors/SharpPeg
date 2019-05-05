using System;
using System.Collections.Generic;
using System.Linq;
using SharpPeg.Operators;
using System.Diagnostics;
using System.Threading.Tasks;
using SharpPeg.Common;
using SharpPeg.Optimizations.Default;

namespace SharpPeg.Compilation
{
    public class Compiler : ICompiler
    {
        public bool InlinePatterns { get; set; } = true;

        public Compiler()
        { }
        
        public CompiledPeg Compile(Pattern StartPattern)
        {
            var patternInfo = PatternInfo.Build(StartPattern);
            var patternIndices = new Dictionary<Pattern, int>();

            var compiledPatterns = new List<Method>();
            var patternsToCompile = new List<Pattern>();

            compiledPatterns.Add(null);
            patternIndices[StartPattern] = 0;
            patternsToCompile.Add(StartPattern);

            foreach(var pattern in patternInfo.Keys)
            {
                if(pattern != StartPattern)
                {
                    var patternId = patternIndices[pattern] = compiledPatterns.Count;
                    compiledPatterns.Add(null);
                    patternsToCompile.Add(pattern);
                }
            }
            
            foreach (var pattern in patternsToCompile)
            {
                compiledPatterns[patternIndices[pattern]] = CompilePattern(pattern, patternInfo, patternIndices);
            }

            return new CompiledPeg(compiledPatterns, 0);
        }

        private Method CompilePattern(Pattern p, Dictionary<Pattern, PatternInfo> patternInfo, Dictionary<Pattern, int> patternIndices)
        {
            if (p is PrecompiledPattern pp)
            {
                var context = new CompilerContext(patternInfo, patternIndices);
                foreach (var instruction in pp.Method.Instructions)
                {
                    if (instruction.Matches(InstructionType.Call))
                    {
                        if (!context.PatternIndices.TryGetValue(pp.PatternReferences[instruction.Data1], out var patternId))
                        {
                            throw new NotImplementedException();
                        }

                        context.Add(Instruction.Call(instruction.Label, (ushort)patternId));
                    }
                    else
                    {
                        context.Add(instruction);
                    }
                }

                return new Method(
                    p.Name,
                    context.ToList(),
                    pp.Method.CharacterRanges,
                    pp.Method.FailureLabelMap,
                    pp.Method.VariableCount,
                    pp.Method.LabelCount
                );
            }
            else
            {
                var context = new CompilerContext(patternInfo, patternIndices);
                var failLabel = context.LabelAllocator++;
                Compile(p.Data, new Dictionary<int, ushort>
                {
                    { LabelMap.DEFAULT_FAIL_LABEL, failLabel },
                }, context);
                context.Flush();

                context.Add(Instruction.Return(0));
                context.Add(Instruction.MarkLabel(failLabel));
                context.Add(Instruction.Return(LabelMap.DEFAULT_FAIL_LABEL));

                return new Method(
                    p.Name,
                    context.ToList(),
                    context.CharacterRanges,
                    context.FailureLabelMap,
                    context.VariableAllocator,
                    context.LabelAllocator
                );
            }
        }

        private void Compile(Operator op, Dictionary<int, ushort> failLabelMap, CompilerContext context)
        {
            switch (op)
            {
                case Empty _:
                    break;
                case Any _:
                    context.UpdateOrSetBoundsCheck(failLabelMap[LabelMap.DEFAULT_FAIL_LABEL], context.DelayedAdvance);
                    context.DelayedAdvance += 1;
                    break;
                case CharacterClass c:
                    context.UpdateOrSetBoundsCheck(failLabelMap[LabelMap.DEFAULT_FAIL_LABEL], context.DelayedAdvance);
                    var pointer = context.GetCharRange(c.Ranges);
                    context.Add(Instruction.Char(failLabelMap[LabelMap.DEFAULT_FAIL_LABEL], context.DelayedAdvance, (ushort)pointer, (ushort)(pointer + c.Ranges.Count)));
                    context.DelayedAdvance += 1;
                    break;
                case Pattern p:
                    context.Flush();
                    if (p.Data == null && !(p is PrecompiledPattern))
                    {
                        throw new CompilationException($"Incomplete pattern definition {p.Name}.");
                    }
                    
                    // Inline if the pattern is small (less than 16 nodes) and does not call any other patterns, or if it only calls another pattern.
                    var patternInfo = context.PatternInfo[p];
                    if (p is PrecompiledPattern 
                        || !InlinePatterns
                        || (!(p.Data is Pattern) && (patternInfo.IsRecursive || patternInfo.NumNodes > 16 || patternInfo.CalledBy.Count > 1)))
                    {
                        if (!context.PatternIndices.TryGetValue(p, out var patternId))
                        {
                            throw new NotImplementedException();
                        }

                        context.Add(Instruction.Call(context.GetFailLabelMap(failLabelMap), (ushort)patternId));
                    }
                    else
                    {
                        Compile(p.Data, failLabelMap, context);
                    }
                    break;
                case Sequence seq:
                    Compile(seq.ChildA, failLabelMap, context);
                    Compile(seq.ChildB, failLabelMap, context);
                    break;
                case Not n:
                    {
                        context.Flush();
                        var innerFailLabel = context.LabelAllocator++;
                        
                        var variable = StorePosition(context);
                        Compile(n.Child, failLabelMap.ExtendWith(LabelMap.DEFAULT_FAIL_LABEL, innerFailLabel), context);
                        context.Flush();

                        context.Add(Instruction.Jump(failLabelMap[LabelMap.DEFAULT_FAIL_LABEL]));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));
                    }
                    break;
                case ZeroOrMore zom:
                    {
                        context.Flush();
                        var startLabel = context.LabelAllocator++;
                        var innerFailLabel = context.LabelAllocator++;

                        context.Add(Instruction.MarkLabel(startLabel));
                        
                        var variable = StorePosition(context);
                        context.InsideLoop++;
                        Compile(zom.Child, failLabelMap.ExtendWith(LabelMap.DEFAULT_FAIL_LABEL, innerFailLabel), context);
                        context.InsideLoop--;
                        context.Flush();

                        context.Add(Instruction.Jump(startLabel));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));
                    }
                    break;
                case PrioritizedChoice pc:
                    {
                        context.Flush();
                        var endLabel = context.LabelAllocator++;
                        var innerFailLabel = context.LabelAllocator++;
                        
                        var variable = StorePosition(context);
                        Compile(pc.ChildA, failLabelMap.ExtendWith(pc.CaughtFailureLabels ?? new[] { LabelMap.DEFAULT_FAIL_LABEL }, innerFailLabel), context);
                        context.Flush();

                        context.Add(Instruction.Jump(endLabel));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));

                        Compile(pc.ChildB, failLabelMap, context);
                        context.Flush();
                        context.Add(Instruction.MarkLabel(endLabel));
                    }
                    break;
                case CaptureGroup cg:
                    {
                        context.Flush();
                        var variable = StorePosition(context);
                        Compile(cg.Child, failLabelMap, context);
                        context.Flush();
                        
                        context.Add(Instruction.Capture(variable, (ushort)cg.Key));
                    }
                    break;
                case Throw th:
                    {
                        if(failLabelMap.TryGetValue(th.FailureLabel, out var targetLabel))
                        {
                            context.Add(Instruction.Jump(targetLabel));
                        } else
                        {
                            context.Add(Instruction.Return((ushort)th.FailureLabel));
                        }
                    }
                    break;
                default:
                    throw new ArgumentException($"The argument {nameof(op)} is '{op}' ({op.GetType()}), which is not one of the recognised PEG types.");
            }
        }
        
        private static ushort StorePosition(CompilerContext context)
        {
            var variable = context.VariableAllocator++;
            context.Add(Instruction.StorePosition(variable));
            return variable;
        }
    }
}
