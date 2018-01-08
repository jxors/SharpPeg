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
            var context = new CompilerContext(patternInfo, patternIndices);
            var failLabel = context.LabelAllocator++;
            Compile(p.Data, failLabel, context);
            context.Flush();

            context.Add(Instruction.Return(1));
            context.Add(Instruction.MarkLabel(failLabel));
            context.Add(Instruction.Return(0));

            return new Method(
                p.Name,
                context.ToList(),
                context.CharacterRanges,
                context.VariableAllocator,
                context.LabelAllocator
            );
        }

        private void Compile(Operator op, ushort failLabel, CompilerContext context)
        {
            switch (op)
            {
                case Empty _:
                    break;
                case Any _:
                    context.UpdateOrSetBoundsCheck(failLabel, context.DelayedAdvance);
                    context.DelayedAdvance += 1;
                    break;
                case CharacterClass c:
                    context.UpdateOrSetBoundsCheck(failLabel, context.DelayedAdvance);
                    var pointer = context.GetCharRange(c.Ranges);
                    context.Add(Instruction.Char(failLabel, context.DelayedAdvance, (ushort)pointer, (ushort)(pointer + c.Ranges.Count)));
                    context.DelayedAdvance += 1;
                    break;
                case Pattern p:
                    context.Flush();
                    if (p.Data == null)
                    {
                        throw new CompilationException($"Incomplete pattern definition {p.Name}.");
                    }
                    
                    // Inline if the pattern is small (less than 16 nodes) and does not call any other patterns, or if it only calls another pattern.
                    var patternInfo = context.PatternInfo[p];
                    if (!(p.Data is Pattern) && (patternInfo.IsRecursive || patternInfo.NumNodes > 16 || patternInfo.PatternCalls.Count > 0))
                    {
                        if (patternInfo.ContainsCaptures)
                        {
                            context.CaptureCount++;
                        }

                        if (!context.PatternIndices.TryGetValue(p, out var patternId))
                        {
                            throw new NotImplementedException();
                        }

                        context.Add(Instruction.Call(failLabel, (ushort)patternId));
                    }
                    else
                    {
                        Compile(p.Data, failLabel, context);
                    }
                    break;
                case Sequence seq:
                    Compile(seq.ChildA, failLabel, context);
                    Compile(seq.ChildB, failLabel, context);
                    break;
                case Not n:
                    {
                        context.Flush();
                        var innerFailLabel = context.LabelAllocator++;

                        var cCountBefore = context.CaptureCount;
                        var variable = StorePosition(context);
                        Compile(n.Child, innerFailLabel, context);
                        context.Flush();

                        context.Add(Instruction.Jump(failLabel));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));

                        if (cCountBefore != context.CaptureCount)
                        {
                            context.CaptureCount = cCountBefore;
                            context.Add(Instruction.DiscardCaptures());
                        }
                    }
                    break;
                case ZeroOrMore zom:
                    {
                        context.Flush();
                        var startLabel = context.LabelAllocator++;
                        var innerFailLabel = context.LabelAllocator++;

                        context.Add(Instruction.MarkLabel(startLabel));

                        var cCountBefore = context.CaptureCount;
                        var variable = StorePosition(context);
                        Compile(zom.Child, innerFailLabel, context);
                        context.Flush();

                        context.Add(Instruction.Jump(startLabel));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));

                        if (cCountBefore != context.CaptureCount)
                        {
                            context.CaptureCount = cCountBefore;
                            context.Add(Instruction.DiscardCaptures());
                        }
                    }
                    break;
                case PrioritizedChoice pc:
                    {
                        context.Flush();
                        var endLabel = context.LabelAllocator++;
                        var innerFailLabel = context.LabelAllocator++;

                        var cCountBefore = context.CaptureCount;
                        var variable = StorePosition(context);
                        Compile(pc.ChildA, innerFailLabel, context);
                        context.Flush();

                        context.Add(Instruction.Jump(endLabel));
                        context.Add(Instruction.MarkLabel(innerFailLabel));
                        context.Add(Instruction.RestorePosition(0, variable));

                        if (cCountBefore != context.CaptureCount)
                        {
                            context.CaptureCount = cCountBefore;
                            context.Add(Instruction.DiscardCaptures());
                        }

                        Compile(pc.ChildB, failLabel, context);
                        context.Flush();
                        context.Add(Instruction.MarkLabel(endLabel));
                    }
                    break;
                case CaptureGroup cg:
                    {
                        context.Flush();
                        var variable = StorePosition(context);
                        Compile(cg.Child, failLabel, context);
                        context.Flush();

                        context.CaptureCount++;
                        context.Add(Instruction.Capture(0, variable, (ushort)cg.Key));
                    }
                    break;
                default:
                    throw new ArgumentException($"The argument {nameof(op)} is '{op}', which is not one of the recognised PEG types.");
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
