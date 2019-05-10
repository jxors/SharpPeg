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

        public bool EnableCaptureMemoization { get; set; } = false;

        private readonly FieldInfo capturesField = typeof(BaseJittedRunner).GetField("captures");
        private readonly FieldInfo resultLabelField = typeof(UnsafePatternResult).GetField("Label");
        private readonly FieldInfo resultPositionField = typeof(UnsafePatternResult).GetField("Position");
        private readonly ConstructorInfo captureConstructor = typeof(TemporaryCapture).GetConstructor(new[] { typeof(int), typeof(int), typeof(char*), typeof(char*) });
        private readonly ConstructorInfo resultConstructor = typeof(UnsafePatternResult).GetConstructor(new[] { typeof(int), typeof(char*) });
        private readonly MethodInfo captureListAddMethod = typeof(List<TemporaryCapture>).GetMethod("Add");
        private readonly MethodInfo captureListCountMethod = typeof(List<TemporaryCapture>).GetMethod("get_Count");
        private readonly FieldInfo dataPtrField = typeof(BaseJittedRunner).GetField("dataPtr");
        private readonly FieldInfo dataEndPtrField = typeof(BaseJittedRunner).GetField("dataEndPtr");
        private readonly FieldInfo dataSizeField = typeof(BaseJittedRunner).GetField("dataSize");
        private readonly FieldInfo entryPointsField = typeof(BaseJittedRunner).GetField("EntryPoints");
        private readonly FieldInfo exitPointsField = typeof(BaseJittedRunner).GetField("ExitPoints");
        private readonly FieldInfo methodsField = typeof(BaseJittedRunner).GetField("Methods");
        private readonly MethodInfo discardCapturesMethod = typeof(BaseJittedRunner).GetMethod("DiscardCaptures", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo charScanNormalMethod = typeof(BaseJittedRunner).GetMethod("CharScanNormal", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo charScan2NormalMethod = typeof(BaseJittedRunner).GetMethod("CharScan2Normal", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo charScan3NormalMethod = typeof(BaseJittedRunner).GetMethod("CharScan3Normal", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo charScan4NormalMethod = typeof(BaseJittedRunner).GetMethod("CharScan4Normal", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly MethodInfo restoreFullMemoize = typeof(BaseJittedRunner).GetMethod("ApplyMemoizedResult", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo doFullMemoize = typeof(BaseJittedRunner).GetMethod("Memoize", BindingFlags.NonPublic | BindingFlags.Instance);

        public IRunner Compile(CompiledPeg peg)
        {
            return (IRunner)Activator.CreateInstance(CompileInternal(peg), new object[] { peg.Methods });
        }

        public IRunnerFactory CompileAsFactory(CompiledPeg peg)
        {
            return new ILRunnerFactory(CompileInternal(peg), peg.Methods);
        }

        private Type CompileInternal(CompiledPeg peg)
        {
            var methods = peg.Methods;
            var moduleBuilder = CreateModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType("InternalType", TypeAttributes.Public | TypeAttributes.Class, typeof(BaseJittedRunner));

            var hookAttributes = MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig;
            var hook = typeBuilder.DefineMethod("RunInternal", hookAttributes, typeof(UnsafePatternResult), new[] { typeof(char*) });
            var methodBuilders = methods
                .Select((method, i) => typeBuilder.DefineMethod($"Pattern_{method}_{i}", MethodAttributes.Private | MethodAttributes.HideBySig, typeof(UnsafePatternResult), new[] { typeof(char*) }))
                .ToArray();
            var memoizationFields = EnableMemoization ? methods.Select((method, i) => typeBuilder.DefineField($"memoization_{method}_{i}", typeof(UnsafePatternResult[]), FieldAttributes.Private)).ToArray() : null;

            BuildHook(hook, peg, methodBuilders, memoizationFields);
            BuildConstructor(typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(List<Method>) }));

            for (var i = 0; i < methodBuilders.Length; i++)
            {
                CompileSinglePattern(i, methods, methodBuilders, memoizationFields);
            }

            return typeBuilder.CreateTypeInfo().UnderlyingSystemType;
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

            if (EmitErrorInfo)
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
                        generator.Emit(OpCodes.Ldc_I4_1);
                        generator.Emit(OpCodes.Add);
                        generator.Emit(OpCodes.Newarr, typeof(UnsafePatternResult));
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
            var resultLocal = DeclareLocal(generator, typeof(UnsafePatternResult), "result");
            var currentCharLocal = new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(char), "currentChar"));
            var memoizationResult = new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(UnsafePatternResult), "memoizationResult"));
            var startCaptureCount = new Lazy<LocalBuilder>(() => DeclareLocal(generator, typeof(int), "startCaptureCount"));
            var memoizeReturnValueLabel = new Lazy<Label>(() => generator.DefineLabel());

            var hasCheckedChain = new bool[method.Instructions.Count];

            generator.BeginScope();
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stloc, positionLocal);

            if (EmitErrorInfo)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, entryPointsField);
                EmitPushInt(generator, index);
                generator.Emit(OpCodes.Ldloc, positionLocal);
                generator.Emit(OpCodes.Stelem, typeof(char*));
            }

            if (EnableMemoization)
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
                generator.Emit(OpCodes.Ldelem, typeof(UnsafePatternResult));
                generator.Emit(OpCodes.Stloc, memoizationResult.Value);
                generator.Emit(OpCodes.Ldloc, memoizationResult.Value);

                // Check if result.Position == null (i.e. we don't have a memoized result yet)
                generator.Emit(OpCodes.Ldfld, resultPositionField);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ceq);
                generator.Emit(OpCodes.Brtrue, noMemoizationLabel);

                if (EnableCaptureMemoization)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, (int)index);

                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, dataPtrField);

                    generator.Emit(OpCodes.Sub);
                    generator.Emit(OpCodes.Ldc_I4_2);
                    generator.Emit(OpCodes.Div);
                    generator.EmitCall(OpCodes.Call, restoreFullMemoize, null);
                }

                generator.Emit(OpCodes.Ldloc, memoizationResult.Value);
                generator.Emit(OpCodes.Ret);

                generator.MarkLabel(noMemoizationLabel);

                if (EnableCaptureMemoization)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, capturesField);
                    generator.Emit(OpCodes.Callvirt, captureListCountMethod);
                    generator.Emit(OpCodes.Stloc, startCaptureCount.Value);
                }
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

                        var successLabel = generator.DefineLabel();

                        // TODO: Use PatternResult to determine where we're going to jump
                        generator.Emit(OpCodes.Stloc, resultLocal);
                        generator.Emit(OpCodes.Ldloc, resultLocal);
                        generator.Emit(OpCodes.Ldfld, resultLabelField);

                        EmitPushInt(generator, 0);
                        generator.Emit(OpCodes.Beq, successLabel);

                        // Special failure case: jump to labels
                        foreach (var (failureLabel, jumpTarget) in method.FailureLabelMap[instruction.Data2].Mapping)
                        {
                            generator.Emit(OpCodes.Ldloc, resultLocal);
                            generator.Emit(OpCodes.Ldfld, resultLabelField);
                            EmitPushInt(generator, failureLabel);
                            generator.Emit(OpCodes.Beq, labels[jumpTarget]);
                        }

                        // General failure case: return failure
                        if (EnableMemoization)
                        {
                            generator.Emit(OpCodes.Br, memoizeReturnValueLabel.Value);
                        }
                        else
                        {
                            generator.Emit(OpCodes.Ldloc, resultLocal);
                            generator.Emit(OpCodes.Ret);
                        }

                        // Success case: use new position returned by pattern
                        generator.MarkLabel(successLabel);
                        generator.Emit(OpCodes.Ldloc, resultLocal);
                        generator.Emit(OpCodes.Ldfld, resultPositionField);
                        generator.Emit(OpCodes.Stloc, positionLocal);

                        break;
                    case InstructionType.Capture:
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldfld, capturesField);

                        // Create capture object
                        {
                            // Push key
                            EmitPushInt(generator, (int)instruction.Data2);

                            // Push OpenIndex
                            generator.Emit(OpCodes.Ldloc, countVariables[instruction.Data1].Value);

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
                        if (instruction.Offset != 0)
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
                            if (EmitErrorInfo)
                            {
                                generator.Emit(OpCodes.Ldarg_0);
                                generator.Emit(OpCodes.Ldfld, exitPointsField);
                                EmitPushInt(generator, index);
                                generator.Emit(OpCodes.Ldloc, positionLocal);
                                generator.Emit(OpCodes.Stelem, typeof(char*));
                            }

                            // Return success
                            generator.Emit(OpCodes.Ldloca, resultLocal);
                            generator.Emit(OpCodes.Ldc_I4_0);
                            generator.Emit(OpCodes.Stfld, resultLabelField);

                            generator.Emit(OpCodes.Ldloca, resultLocal);
                            generator.Emit(OpCodes.Ldloc, positionLocal);
                            generator.Emit(OpCodes.Stfld, resultPositionField);
                        }
                        else
                        {
                            // Return failure
                            generator.Emit(OpCodes.Ldloca, resultLocal);
                            EmitPushInt(generator, instruction.Data1);
                            generator.Emit(OpCodes.Stfld, resultLabelField);

                            generator.Emit(OpCodes.Ldloca, resultLocal);
                            generator.Emit(OpCodes.Ldloc, positionLocal);
                            generator.Emit(OpCodes.Stfld, resultPositionField);
                        }

                        if (EnableMemoization)
                        {
                            generator.Emit(OpCodes.Br, memoizeReturnValueLabel.Value);
                        }
                        else
                        {
                            generator.Emit(OpCodes.Ldloc, resultLocal);
                            generator.Emit(OpCodes.Ret);
                        }
                        break;
                    case InstructionType.MarkLabel:
                        generator.MarkLabel(labels[instruction.Label]);
                        var info = DetectCharScan(method, i);
                        if (info != null)
                        {
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldloc, positionLocal);
                            if (info.StartOffset != 0)
                            {
                                EmitPushInt(generator, info.StartOffset * 2);
                                generator.Emit(OpCodes.Add);
                            }

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldfld, dataEndPtrField);
                            if(info.Bounds != 0)
                            {
                                EmitPushInt(generator, info.Bounds * 2);
                                generator.Emit(OpCodes.Sub);
                            }

                            foreach (var c in info.SearchFor)
                            {
                                // TODO: Pre-generate mask
                                EmitPushInt(generator, c);
                            }

                            if (info.SearchFor.Count == 1)
                            {
                                generator.EmitCall(OpCodes.Call, charScanNormalMethod, null);
                            }
                            else if (info.SearchFor.Count == 2)
                            {
                                generator.EmitCall(OpCodes.Call, charScan2NormalMethod, null);
                            }
                            else if (info.SearchFor.Count == 3)
                            {
                                generator.EmitCall(OpCodes.Call, charScan3NormalMethod, null);
                            }
                            else if (info.SearchFor.Count == 4)
                            {
                                generator.EmitCall(OpCodes.Call, charScan4NormalMethod, null);
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }

                            if (info.StartOffset != 0)
                            {
                                EmitPushInt(generator, info.StartOffset * 2);
                                generator.Emit(OpCodes.Sub);
                            }
                            generator.Emit(OpCodes.Stloc, positionLocal);
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unrecognised instruction type: {instruction.Type}");
                }
            }

            if (EnableMemoization)
            {
                generator.MarkLabel(memoizeReturnValueLabel.Value);
                generator.Emit(OpCodes.Ldloc, resultLocal);
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
                generator.Emit(OpCodes.Stelem, typeof(UnsafePatternResult));

                if (EnableCaptureMemoization)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, index);

                    generator.Emit(OpCodes.Ldarg_1);

                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, dataPtrField);

                    generator.Emit(OpCodes.Sub);
                    generator.Emit(OpCodes.Ldc_I4_2);
                    generator.Emit(OpCodes.Div);

                    generator.Emit(OpCodes.Ldloc, startCaptureCount.Value);
                    generator.EmitCall(OpCodes.Call, doFullMemoize, null);
                }

                generator.Emit(OpCodes.Ldloc, resultLocal);
                generator.Emit(OpCodes.Ret);
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

        private CharScanInfo DetectCharScan(Method method, int startPos)
        {
            var instructions = method.Instructions;
            var pos = startPos;
            if (instructions[pos].Matches(InstructionType.MarkLabel, out var label))
            {
                var loopInstructions = new List<Instruction>();
                var loops = false;
                var hasHadBoundsCheck = false;
                for (var j = 0; j < 8; j++)
                {
                    var instr = instructions[pos];
                    loopInstructions.Add(instructions[pos]);

                    if (instr.Matches(InstructionType.Return))
                    {
                        return null;
                    }

                    if(instr.Matches(InstructionType.BoundsCheck))
                    {
                        hasHadBoundsCheck = true;
                    }else if((instr.Matches(InstructionType.Char) || instr.Matches(InstructionType.Advance)) && !hasHadBoundsCheck)
                    {
                        return null;
                    }

                    if (!instr.Matches(InstructionType.BoundsCheck)
                        && instr.CanJumpToLabel)
                    {
                        pos = GetLabelPosition(instructions, instr.Label);
                    }
                    else
                    {
                        pos += 1;
                    }

                    if (pos == startPos)
                    {
                        loops = true;
                        break;
                    }
                }

                if(!loops)
                {
                    return null;
                }

                if (!loopInstructions.All(instr =>
                      (instr.Type == InstructionType.Advance && instr.Offset == 1)
                      || instr.Type == InstructionType.Char
                      || instr.Type == InstructionType.Jump
                      || instr.Type == InstructionType.BoundsCheck
                      || instr.Type == InstructionType.MarkLabel))
                {
                    return null;
                }

                if (loopInstructions.Count(instr => instr.Type == InstructionType.Advance) != 1)
                {
                    return null;
                }

                var charRanges = loopInstructions
                    .Where(instr => instr.Type == InstructionType.Char)
                    .SelectMany(instr => method.CharacterRanges.Skip(instr.Data1).Take(instr.Data2 - instr.Data1))
                    .ToList();

                // TODO: Check if we can generate an efficient matching mask (i.e. some bits that are equal in all of the characters)
                if(charRanges.Sum(range => range.Max - range.Min + 1) > 4)
                {
                    return null;
                }

                // Additional conditions for now that are not really needed:
                var lookupOffset = loopInstructions.First(item => item.Type == InstructionType.Char).Offset;
                if(!loopInstructions.All(instr => (instr.Type != InstructionType.Char || instr.Offset == lookupOffset)))
                {
                    return null;
                }

                var bounds = 0;
                var advanced = 0;
                foreach(var instr in loopInstructions)
                {
                    if(instr.Matches(InstructionType.BoundsCheck, out var _, out var offset) && offset + advanced > bounds)
                    {
                        bounds = offset + advanced;
                    } else if(instr.Matches(InstructionType.Advance, out var _, out var advanceOffset))
                    {
                        advanced += advanceOffset;
                    }
                }

                var chars = new List<char>();
                foreach(var range in charRanges)
                {
                    for (var i = range.Min; i <= range.Max; i++)
                    {
                        chars.Add(i);
                    }
                }

                return new CharScanInfo(bounds, lookupOffset, chars);
            }

            return null;
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
