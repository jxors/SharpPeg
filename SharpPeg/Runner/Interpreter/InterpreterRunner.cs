﻿using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpPeg.Runner.Interpreter
{
    public class InterpreterRunner : IRunner
    {
        private int startMethodIndex = 0;
        private IReadOnlyList<Method> Methods;
        private char[] Data;
        private int EndPos;
        private List<Capture> CaptureOutput;
        private int[][] LabelPositions;

        public InterpreterRunner(int startMethodIndex, IReadOnlyList<Method> methods, int[][] labelPositions)
        {
            this.startMethodIndex = startMethodIndex;
            this.Methods = methods;
            this.LabelPositions = labelPositions;
        }

        public string ExplainResult(RunResult result, string inputData) => throw new NotImplementedException();

        public RunResult Run(string stringData, List<Capture> captureOutput = null) => Run(stringData.ToCharArray(), 0, stringData.Length, captureOutput);

        public RunResult Run(char[] data, int index, int length, List<Capture> captureOutput = null)
        {
            Data = data;
            EndPos = index + length;
            CaptureOutput = captureOutput;

            var result = InternalRun(Methods[0], LabelPositions[0], index);
            CaptureOutput?.Sort();
            var successful = result != -1;
            return new RunResult(successful, successful ? result : 0, 0);
        }

        private int InternalRun(Method method, int[] labelPositions, int pos)
        {
            var instructions = method.Instructions;
            var variables = new int[method.VariableCount];
            var offsets = new int[method.VariableCount];
            var pc = 0;

            while(true)
            {
                var instr = instructions[pc++];
                switch (instr.Type)
                {
                    case InstructionType.Advance:
                        pos += instr.Offset;
                        break;
                    case InstructionType.BoundsCheck:
                        if(pos + instr.Offset >= EndPos)
                        {
                            pc = labelPositions[instr.Label];
                        }
                        break;
                    case InstructionType.Char:
                        var c = Data[pos + instr.Offset];

                        for(var i = instr.Data1; i < instr.Data2; i++)
                        {
                            var range = method.CharacterRanges[i];
                            if (c >= range.Min && c <= range.Max)
                            {
                                goto success;
                            }
                        }

                        // Matching failed
                        pc = labelPositions[instr.Label];

                        success:
                        break;
                    case InstructionType.Capture:
                        CaptureOutput.Add(new Capture(instr.Data2, variables[instr.Data1], pos, offsets[instr.Data1], CaptureOutput.Count));
                        break;
                    case InstructionType.Jump:
                        pc = labelPositions[instr.Label];
                        break;
                    case InstructionType.StorePosition:
                        variables[instr.Data1] = pos;
                        offsets[instr.Data1] = CaptureOutput?.Count ?? 0;
                        break;
                    case InstructionType.RestorePosition:
                        pos = variables[instr.Data1] + instr.Offset;

                        if (CaptureOutput != null)
                        {
                            var offset = offsets[instr.Data1];
                            if (CaptureOutput.Count > offset)
                            {
                                CaptureOutput.RemoveRange(offset, CaptureOutput.Count - offset);
                            }
                        }
                        break;
                    case InstructionType.Call:
                        pos = InternalRun(Methods[instr.Data1], LabelPositions[instr.Data1], pos);
                        if(pos == -1)
                        {
                            pc = labelPositions[instr.Label];
                        }
                        break;
                    case InstructionType.Return: return instr.Data1 == 0  ? -1 : pos;
                    case InstructionType.MarkLabel: break;
                    default: throw new NotImplementedException();
                }
            }
        }
    }
}