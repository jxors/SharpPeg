using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using SharpPeg.Operators;
using SharpPeg.Common;

namespace SharpPeg.Runner.ILRunner
{
    public class ILJitter : IJitter
    {
        struct CharRange
        {
            public char Min, Max;

            public bool IsSingleChar => Min == Max;

            public CharRange(char min, char max)
            {
                Min = min;
                Max = max;
            }

            public override string ToString()
            {
                return $"{Min}-{Max}";
            }
        }

        public bool EmitErrorInfo { get; set; } = true;

        public bool EnableMemoization { get; set; } = false;

        private readonly FieldInfo capturesField = typeof(BaseJittedRunner).GetField("captures");
        private readonly ConstructorInfo captureConstructor = typeof(TemporaryCapture).GetConstructor(new[] { typeof(int), typeof(char*), typeof(char*) });
        private readonly MethodInfo captureListAddMethod = typeof(List<TemporaryCapture>).GetMethod("Add");
        private readonly MethodInfo captureListCountMethod = typeof(List<TemporaryCapture>).GetMethod("get_Count");
        private readonly FieldInfo dataPtrField = typeof(BaseJittedRunner).GetField("dataPtr");
        private readonly FieldInfo dataEndPtrField = typeof(BaseJittedRunner).GetField("dataEndPtr");
        private readonly FieldInfo dataSizeField = typeof(BaseJittedRunner).GetField("dataSize");
        private readonly FieldInfo entryPointsField = typeof(BaseJittedRunner).GetField("EntryPoints");
        private readonly FieldInfo exitPointsField = typeof(BaseJittedRunner).GetField("ExitPoints");
        private readonly FieldInfo methodsField = typeof(BaseJittedRunner).GetField("Methods");
        private readonly MethodInfo discardCapturesMethod = typeof(BaseJittedRunner).GetMethod("DiscardCaptures", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public IRunner Compile(CompiledPeg peg)
        {
            var methods = peg.Methods;
            var moduleBuilder = CreateModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType("InternalType", TypeAttributes.Public | TypeAttributes.Class, typeof(BaseJittedRunner));

            var hookAttributes = MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig;
            var hook = typeBuilder.DefineMethod("RunInternal", hookAttributes, typeof(char*), new[] { typeof(char*) });
            var methodBuilders = methods
                .Select((method, i) => typeBuilder.DefineMethod($"Pattern_{method}_{i}", MethodAttributes.Private | MethodAttributes.HideBySig, typeof(char*), new[] { typeof(char*) }))
                .ToArray();
            var memoizationFields = EnableMemoization ? methods.Select((method, i) => typeBuilder.DefineField($"memoization_{method}_{i}", typeof(char*[]), FieldAttributes.Private)).ToArray() : null;

            BuildHook(hook, peg, methodBuilders, memoizationFields);
            BuildConstructor(typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(List<Method>) }));

            for (var i = 0; i < methodBuilders.Length; i++)
            {
                CompileSinglePattern(i, methods, methodBuilders, memoizationFields);
            }
            
            var m_Type = typeBuilder.CreateTypeInfo();

            return (IRunner)Activator.CreateInstance(m_Type.UnderlyingSystemType, new object[] { methods });
        }

        private void BuildConstructor(ConstructorBuilder constructorBuilder)
        {
            var generator = constructorBuilder.GetILGenerator();
            
            generator.BeginScope();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, methodsField);
            generator.Emit(OpCodes.Ret);
            generator.EndScope();
        }

        private void BuildHook(MethodBuilder methodBuilder, CompiledPeg peg, MethodBuilder[] methodBuilders, FieldBuilder[] memoizationFields)
        {
            var generator = methodBuilder.GetILGenerator();

            if(EmitErrorInfo)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, peg.Methods.Count);
                generator.Emit(OpCodes.Newarr, typeof(char*));
                generator.Emit(OpCodes.Stfld, entryPointsField);

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, peg.Methods.Count);
                generator.Emit(OpCodes.Newarr, typeof(char*));
                generator.Emit(OpCodes.Stfld, exitPointsField);
            }

            if (EnableMemoization)
            {
                foreach (var field in memoizationFields)
                {
                    if (field != null)
                    {
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, dataSizeField);
                        generator.Emit(OpCodes.Newarr, typeof(char*));
                        generator.Emit(OpCodes.Stfld, field);
                    }
                }
            }

            generator.BeginScope();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.EmitCall(OpCodes.Call, methodBuilders[peg.StartPatternIndex], null);
            generator.Emit(OpCodes.Ret);

            generator.EndScope();
        }

        protected virtual ModuleBuilder CreateModuleBuilder()
        {
            var assemblyName = new AssemblyName("DynamicPegRunner");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            return assemblyBuilder.DefineDynamicModule("DynamicPegRunner");
        }

        protected virtual LocalBuilder DeclareLocal(ILGenerator gen, Type type, string name)
        {
            return gen.DeclareLocal(type);
        }

        protected virtual void MarkSequencePoint(ILGenerator gen, Method method, int instructionIndex) { }

        protected virtual void BeginCompile(Method methodBuilder) { }

        private void CompileSinglePattern(int index, IReadOnlyList<Method> methods, MethodBuilder[] methodBuilders, FieldBuilder[] memoizationFields)
        {
            var method = methods[index];
            var methodBuilder = methodBuilders[index];

            BeginCompile(method);

            var generator = methodBuilder.GetILGenerator();
            var variables = Enumerable.Range(0, method.VariableCount).Select(item => new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(char*), $"var_{item}"))).ToArray();
            var countVariables = Enumerable.Range(0, method.VariableCount).Select(item => new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(char*), $"count_{item}"))).ToArray();
            var labels = Enumerable.Range(0, method.LabelCount).Select(item => generator.DefineLabel()).ToArray();
            
            var positionLocal = DeclareLocal(generator, typeof(char*), "position");
            var currentCharLocal = new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(char), "currentChar"));
            var memoizationResult = new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(char*), "memoizationResult"));

            var hasCheckedChain = new bool[method.Instructions.Count];

            generator.BeginScope();
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stloc, positionLocal);

            if(EmitErrorInfo)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, entryPointsField);
                EmitPushInt(generator, index);
                generator.Emit(OpCodes.Ldloc, positionLocal);
                generator.Emit(OpCodes.Stelem, typeof(char*));
            }

            if(EnableMemoization)
            {
                var noMemoizationLabel = generator.DefineLabel();
                
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, memoizationFields[index]);
                
                // Calculate position
                generator.Emit(OpCodes.Ldarg_1);

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, dataPtrField);

                generator.Emit(OpCodes.Sub);
                generator.Emit(OpCodes.Ldc_I4_2);
                generator.Emit(OpCodes.Div);

                // Load memoization result
                generator.Emit(OpCodes.Ldelem, typeof(char*));
                generator.Emit(OpCodes.Stloc, memoizationResult.Value);
                generator.Emit(OpCodes.Ldloc, memoizationResult.Value);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brtrue, noMemoizationLabel);

                generator.Emit(OpCodes.Ldloc, memoizationResult.Value);
                generator.Emit(OpCodes.Ldc_I4_1);
                generator.Emit(OpCodes.Sub);
                generator.Emit(OpCodes.Ret);

                generator.MarkLabel(noMemoizationLabel);
            }

            const int CallFailed = 0;
            for (var i = 0; i < method.Instructions.Count; i++)
            {
                var instruction = method.Instructions[i];
                MarkSequencePoint(generator, method, i);

                switch (instruction.Type)
                {
                    case InstructionType.Advance:
                        generator.Emit(OpCodes.Ldloc, positionLocal);
                        EmitPushInt(generator, instruction.Offset * sizeof(char));
                        generator.Emit(OpCodes.Add);
                        generator.Emit(OpCodes.Stloc, positionLocal);
                        break;
                    case InstructionType.BoundsCheck:
                        generator.Emit(OpCodes.Ldloc, positionLocal);

                        if (instruction.Offset > 0)
                        {
                            EmitPushInt(generator, instruction.Offset * sizeof(char));
                            generator.Emit(OpCodes.Add);
                        }

                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, dataEndPtrField);

                        if (method.Instructions[i + 1].Matches(InstructionType.Jump, out var jumpLabel))
                        {
                            /* Invert BoundsChecks like this:
                             * 
                             * | BoundsCheck N L_fail
                             * | Jump L_success
                             * 
                             * Reasoning for this is that we expect the BoundsCheck to usually succeed, so in the general case,
                             * inverting the check will execute one unconditional jump less. 
                             */
                            generator.Emit(OpCodes.Blt_Un, labels[jumpLabel]);
                            generator.Emit(OpCodes.Br, labels[instruction.Label]);
                            i += 1;
                        }
                        else
                        {
                            generator.Emit(OpCodes.Bge_Un, labels[instruction.Label]);
                        }
                        break;
                    case InstructionType.Char:
                        generator.Emit(OpCodes.Ldloc, positionLocal);
                        if (instruction.Offset != 0)
                        {
                            EmitPushInt(generator, instruction.Offset * sizeof(char));
                            generator.Emit(OpCodes.Add);
                        }

                        var chain = FindChainAt(method.Instructions, method, method.Instructions[i].Offset, i, out var min, out var max, out var lastLabel, out var coverage);
                        if (!hasCheckedChain[i] && chain.Count >= 3 && coverage < (max - min) / 2)
                        {
                            MarkChain(method.Instructions, i, lastLabel, hasCheckedChain);

                            // Chain check
                            generator.Emit(OpCodes.Ldind_U2);
                            generator.Emit(OpCodes.Stloc, currentCharLocal.Value);
                            
                            generator.Emit(OpCodes.Ldloc, currentCharLocal.Value);
                            EmitPushInt(generator, min);
                            generator.Emit(OpCodes.Sub);
                            EmitPushInt(generator, max - min);
                            generator.Emit(OpCodes.Bgt_Un, labels[lastLabel]);
                            
                            EmitCharacterClassCheck(generator, method, true, currentCharLocal, instruction.Data1, instruction.Data2, true, labels[instruction.Label]);
                        }
                        else
                        {
                            EmitCharacterClassCheck(generator, method, false, currentCharLocal, instruction.Data1, instruction.Data2, true, labels[instruction.Label]);
                        }
                        break;
                    case InstructionType.Jump:
                        generator.Emit(OpCodes.Br, labels[instruction.Label]);
                        break;
                    case InstructionType.Call:
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldloc, positionLocal);
                        generator.EmitCall(OpCodes.Call, methodBuilders[instruction.Data1], null);

                        generator.Emit(OpCodes.Stloc, positionLocal);
                        generator.Emit(OpCodes.Ldloc, positionLocal);

                        EmitPushInt(generator, CallFailed);
                        generator.Emit(OpCodes.Beq, labels[instruction.Label]);
                        break;
                    case InstructionType.Capture:
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, capturesField);

                        // Create capture object
                        {
                            // Push key
                            EmitPushInt(generator, (int)instruction.Data2);

                            // Push startIndex
                            generator.Emit(OpCodes.Ldloc, variables[instruction.Data1].Value);

                            // Push endIndex
                            generator.Emit(OpCodes.Ldloc, positionLocal);

                            // Create object
                            generator.Emit(OpCodes.Newobj, captureConstructor);
                        }

                        // Insert in captures
                        generator.Emit(OpCodes.Callvirt, captureListAddMethod);
                        break;
                    case InstructionType.StorePosition:
                        generator.Emit(OpCodes.Ldloc, positionLocal);
                        generator.Emit(OpCodes.Stloc, variables[instruction.Data1].Value);

                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, capturesField);
                        generator.Emit(OpCodes.Callvirt, captureListCountMethod);
                        generator.Emit(OpCodes.Stloc, countVariables[instruction.Data1].Value);
                        break;
                    case InstructionType.RestorePosition:
                        generator.Emit(OpCodes.Ldloc, variables[instruction.Data1].Value);
                        if(instruction.Offset != 0)
                        {
                            EmitPushInt(generator, instruction.Offset * sizeof(char));
                            generator.Emit(OpCodes.Add);
                        }

                        generator.Emit(OpCodes.Stloc, positionLocal);

                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldloc, countVariables[instruction.Data1].Value);
                        generator.EmitCall(OpCodes.Call, discardCapturesMethod, null);
                        break;
                    case InstructionType.Return:
                        if (instruction.Data1 == 0)
                        {
                            // Return fail
                            generator.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            if (EmitErrorInfo)
                            {
                                generator.Emit(OpCodes.Ldarg_0);
                                generator.Emit(OpCodes.Ldfld, exitPointsField);
                                EmitPushInt(generator, index);
                                generator.Emit(OpCodes.Ldloc, positionLocal);
                                generator.Emit(OpCodes.Stelem, typeof(char*));
                            }

                            // Return success
                            generator.Emit(OpCodes.Ldloc, positionLocal);
                        }

                        if(EnableMemoization)
                        {
                            generator.Emit(OpCodes.Stloc, memoizationResult.Value);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldfld, memoizationFields[index]);

                            // Calculate position
                            generator.Emit(OpCodes.Ldarg_1);

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldfld, dataPtrField);

                            generator.Emit(OpCodes.Sub);
                            generator.Emit(OpCodes.Ldc_I4_2);
                            generator.Emit(OpCodes.Div);

                            // Store
                            generator.Emit(OpCodes.Ldloc, memoizationResult.Value);
                            generator.Emit(OpCodes.Ldc_I4_1);
                            generator.Emit(OpCodes.Add);
                            generator.Emit(OpCodes.Stelem, typeof(char*));

                            generator.Emit(OpCodes.Ldloc, memoizationResult.Value);
                        }

                        generator.Emit(OpCodes.Ret);
                        break;
                    case InstructionType.MarkLabel:
                        generator.MarkLabel(labels[instruction.Label]);
                        break;
                    default:
                        throw new ArgumentException($"Unrecognised instruction type: {instruction.Type}");
                }
            }
            
            generator.EndScope();
        }

        private static void EmitCharacterClassCheck(ILGenerator generator, Method method, bool didStore, Lazy<LocalBuilder> currentCharLocal, ushort pointer, ushort endPointer, bool jumpOnFail, Label targetLabel)
        {
            if (pointer + 1 == endPointer)
            {
                if(didStore)
                {
                    generator.Emit(OpCodes.Ldloc, currentCharLocal.Value);
                }
                else
                {
                    generator.Emit(OpCodes.Ldind_U2);
                }

                var range = method.CharacterRanges[pointer];
                EmitCharacterRangeCheck(generator, range.Min, range.Max, jumpOnFail, targetLabel);
            }
            else
            {
                if(!didStore)
                {
                    generator.Emit(OpCodes.Ldind_U2);
                    generator.Emit(OpCodes.Stloc, currentCharLocal.Value);
                }

                var endLabel = jumpOnFail ? generator.DefineLabel() : targetLabel;
                for (var i = pointer; i < endPointer; i++)
                {
                    generator.Emit(OpCodes.Ldloc, currentCharLocal.Value);
                    var range = method.CharacterRanges[i];
                    EmitCharacterRangeCheck(generator, range.Min, range.Max, false, endLabel);
                }

                if (jumpOnFail)
                {
                    generator.Emit(OpCodes.Br, targetLabel);
                    generator.MarkLabel(endLabel);
                }
            }
        }

        private static void EmitCharacterRangeCheck(ILGenerator generator, char minChar, char maxChar, bool jumpOnFail, Label targetLabel)
        {
            if (minChar == maxChar)
            {
                EmitPushInt(generator, minChar);
                generator.Emit(jumpOnFail ? OpCodes.Bne_Un : OpCodes.Beq, targetLabel);
            }
            else if (minChar == char.MinValue)
            {
                EmitPushInt(generator, maxChar);
                generator.Emit(jumpOnFail ? OpCodes.Bgt : OpCodes.Ble, targetLabel);
            }
            else if (maxChar == char.MaxValue)
            {
                EmitPushInt(generator, minChar);
                generator.Emit(jumpOnFail ? OpCodes.Blt : OpCodes.Bge, targetLabel);
            }
            else
            {
                EmitPushInt(generator, minChar);
                generator.Emit(OpCodes.Sub);
                EmitPushInt(generator, maxChar - minChar);
                generator.Emit(jumpOnFail ? OpCodes.Bgt_Un : OpCodes.Ble_Un, targetLabel);
            }
        }

        private void MarkChain(IReadOnlyList<Instruction> instructions, int i, ushort lastLabel, bool[] markers)
        {
            var length = 0;

            while (instructions[i].Matches(InstructionType.Char, out var label))
            {
                markers[i] = true;
                length++;
                i = GetLabelPosition(instructions, label) + 1;
                if(label == lastLabel)
                {
                    break;
                }
            }
        }

        private List<Instruction> FindChainAt(IReadOnlyList<Instruction> instructions, Method method, short offset, int i, out char min, out char max, out ushort lastLabel, out int coverage)
        {
            var items = new List<Instruction>();
            min = char.MaxValue;
            max = char.MinValue;
            lastLabel = ushort.MaxValue;
            coverage = 0;

            while (instructions[i].Matches(InstructionType.Char, out var label, offset, out var pointerStart, out var pointerEnd))
            {
                for(var j = pointerStart; j < pointerEnd; j++)
                {
                    var range = method.CharacterRanges[j];
                    if(range.Min < min)
                    {
                        min = range.Min;
                    }

                    if(range.Max > max)
                    {
                        max = range.Max;
                    }

                    coverage += (range.Max - range.Min) + 1;
                }

                items.Add(instructions[i]);
                lastLabel = label;
                i = GetLabelPosition(instructions, label) + 1;
            }

            return items;
        }

        protected static int GetLabelPosition(IReadOnlyList<Instruction> instructions, ushort label)
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Matches(InstructionType.MarkLabel, label))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void EmitPushInt(ILGenerator generator, int value)
        {
            if (value >= -128 && value <= 127)
            {
                generator.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
            }
            else
            {
                generator.Emit(OpCodes.Ldc_I4, value);
            }
        }
    }
}
