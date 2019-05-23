using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
#if NET_CORE_30
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace SharpPeg.Runner.ILRunner
{
    public class CharScanInfo
    {
        public IReadOnlyList<char> SearchFor { get; }
        public int StartOffset { get; }
        public int Bounds { get; }

        public CharScanInfo(int bounds, int startOffset, IReadOnlyList<char> searchFor)
        {
            this.Bounds = bounds;
            this.StartOffset = startOffset;
            this.SearchFor = searchFor;
        }

        public void Emit(ILGenerator generator, LocalBuilder positionLocal, LocalBuilder endLocal)
        {
#if NET_CORE_30
            if (Avx2.IsSupported)
            {
                GenerateScanLoopAvx2(generator, positionLocal, endLocal);
            }
            else
#endif
            {
                GenerateScanLoopBase(generator, positionLocal, endLocal);
            }
        }

        /*
        if (Avx.IsSupported)
        {
            var pos = startPos;
            var mask1 = Vector256.Create(searchFor1);
            var mask2 = Vector256.Create(searchFor2);
            while (pos < endPos)
            {
                var other = Avx2.LoadVector256((byte*)pos).As<byte, ushort>();
                var result1 = Avx2.CompareEqual(mask1, other);
                var resultMask1 = Avx2.MoveMask(result1.As<ushort, byte>());
                var result2 = Avx2.CompareEqual(mask2, other);
                var resultMask2 = Avx2.MoveMask(result2.As<ushort, byte>());
                var resultMask = resultMask1 | resultMask2;
                if (resultMask != 0)
                {
                    while((resultMask & 1) == 0) { pos++; resultMask >>= 1; }
                    return pos;
                }
                else
                {
                    pos += 16;
                }
            }
        }*/
        private void GenerateScanLoopAvx2(ILGenerator generator, LocalBuilder positionLocal, LocalBuilder endLocal)
        {
#if NET_CORE_30
            var masks = SearchFor.Select(m => generator.DeclareLocal(typeof(Vector256<ushort>))).ToList();
            var loopCondition = generator.DefineLabel();
            var loopBody = generator.DefineLabel();
            var end = generator.DefineLabel();
            var zeroes = generator.DeclareLocal(typeof(int));
            var line = generator.DeclareLocal(typeof(Vector256<ushort>));
            var x = generator.DeclareLocal(typeof(Vector256<byte>));

            foreach(var (mask, c) in masks.Zip(SearchFor))
            {
                generator.Emit(OpCodes.Ldc_I4, c);
                generator.EmitCall(OpCodes.Call, typeof(Vector256).GetMethod("Create", new[] { typeof(char) }), null);
                generator.Emit(OpCodes.Stloc, mask);
            }

            generator.Emit(OpCodes.Br, loopCondition);
            generator.MarkLabel(loopBody);

            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.EmitCall(OpCodes.Call, typeof(Avx).GetMethod("LoadVector256", new[] { typeof(byte*) }), null);
            generator.EmitCall(OpCodes.Call, typeof(Vector256).GetMethod("As").MakeGenericMethod(typeof(byte), typeof(ushort)), null);
            generator.Emit(OpCodes.Stloc, line);

            foreach (var mask in masks)
            {
                // x = (line ^ mask)
                generator.Emit(OpCodes.Ldloc, line);
                generator.Emit(OpCodes.Ldloc, mask);
                generator.EmitCall(OpCodes.Call, typeof(Avx2).GetMethod("CompareEqual", new[] { typeof(Vector256<ushort>), typeof(Vector256<ushort>) }), null);
                generator.EmitCall(OpCodes.Call, typeof(Vector256).GetMethod("As").MakeGenericMethod(typeof(ushort), typeof(byte)), null);
                generator.EmitCall(OpCodes.Call, typeof(Avx2).GetMethod("MoveMask", new[] { typeof(Vector256<byte>) }), null);
            }

            foreach (var mask in masks.Skip(1))
            {
                generator.Emit(OpCodes.Or);
            }

            generator.Emit(OpCodes.Stloc, zeroes);

            var notFound = generator.DefineLabel();
            var innerLoopCondition = generator.DefineLabel();
            var innerLoopBody = generator.DefineLabel();
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Brfalse, notFound);

            generator.Emit(OpCodes.Br, innerLoopCondition);
            // zeroes >>= 1;
            generator.MarkLabel(innerLoopBody);
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Ldc_I4_2);
            generator.Emit(OpCodes.Shr_Un);
            generator.Emit(OpCodes.Stloc, zeroes);

            // position += 1;
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldc_I4_2);
            generator.Emit(OpCodes.Conv_I);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stloc, positionLocal);

            // if (zeroes & 3) == 0, loop
            generator.MarkLabel(innerLoopCondition);
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Ldc_I4_3);
            generator.Emit(OpCodes.And);
            generator.Emit(OpCodes.Brfalse, innerLoopBody);
            generator.Emit(OpCodes.Br, end);

            generator.MarkLabel(notFound);

            // position += 16;
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldc_I4, 32);
            generator.Emit(OpCodes.Conv_I);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stloc, positionLocal);

            //// if position < endPosition, loop
            generator.MarkLabel(loopCondition);
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldloc, endLocal);
            generator.Emit(OpCodes.Clt_Un);
            generator.Emit(OpCodes.Brtrue, loopBody);

            generator.MarkLabel(end);
#endif
        }

        /*
        var mask1 = ~(searchFor1 | ((ulong)searchFor1 << 16) | ((ulong)searchFor1 << 32) | ((ulong)searchFor1 << 48));
        var mask2 = ~(searchFor2 | ((ulong)searchFor2 << 16) | ((ulong)searchFor2 << 32) | ((ulong)searchFor2 << 48));
        var pos = startPos;
        while (pos < endPos)
        {
            var line = *(ulong*)pos;
            {
                var x = (line ^ mask1);
                var t0 = (x & 0x7fff7fff7fff7fffLU) + 0x0001000100010001LU;
                var t1 = (x & 0x8000800080008000LU);
                var zeroes = t0 & t1;
                if (zeroes != 0)
                {
                    while ((ushort)zeroes == 0) { pos++; zeroes >>= 16; }
                    return pos;
                }
            }

            {
                var x = (line ^ mask2);
                var t0 = (x & 0x7fff7fff7fff7fffLU) + 0x0001000100010001LU;
                var t1 = (x & 0x8000800080008000LU);
                var zeroes = t0 & t1;
                if (zeroes != 0)
                {
                    while ((ushort)zeroes == 0) { pos++; zeroes >>= 16; }
                    return pos;
                }
            }

            pos += 4;
        }
        */
        private void GenerateScanLoopBase(ILGenerator generator, LocalBuilder positionLocal, LocalBuilder endLocal)
        {
            var masks = SearchFor.Select(c => ~(c | ((ulong)c << 16) | ((ulong)c << 32) | ((ulong)c << 48)));
            var loopCondition = generator.DefineLabel();
            var loopBody = generator.DefineLabel();
            var end = generator.DefineLabel();
            var zeroes = generator.DeclareLocal(typeof(ulong));
            var line = generator.DeclareLocal(typeof(ulong));
            var x = generator.DeclareLocal(typeof(ulong));

            generator.Emit(OpCodes.Br, loopCondition);
            generator.MarkLabel(loopBody);

            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldind_I8);
            generator.Emit(OpCodes.Stloc, line);

            foreach (var mask in masks)
            {
                // x = (line ^ mask)
                generator.Emit(OpCodes.Ldloc, line);
                generator.Emit(OpCodes.Ldc_I8, (long)mask);
                generator.Emit(OpCodes.Xor);
                generator.Emit(OpCodes.Stloc, x);

                // (x & 0x7fff7fff7fff7fffLU) + 0x0001000100010001LU
                generator.Emit(OpCodes.Ldloc, x);
                generator.Emit(OpCodes.Ldc_I8, 0x7fff7fff7fff7fffL);
                generator.Emit(OpCodes.And);
                generator.Emit(OpCodes.Ldc_I8, 0x0001000100010001L);
                generator.Emit(OpCodes.Add);

                // (x & 0x8000800080008000LU)
                generator.Emit(OpCodes.Ldloc, x);
                generator.Emit(OpCodes.Ldc_I8, unchecked((long)0x8000800080008000L));
                generator.Emit(OpCodes.And);

                generator.Emit(OpCodes.And);
            }

            foreach (var mask in masks.Skip(1))
            {
                generator.Emit(OpCodes.Or);
            }

            generator.Emit(OpCodes.Stloc, zeroes);

            var notFound = generator.DefineLabel();
            var innerLoopCondition = generator.DefineLabel();
            var innerLoopBody = generator.DefineLabel();
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Brfalse, notFound);

            generator.Emit(OpCodes.Br, innerLoopCondition);
            // zeroes >>= 16;
            generator.MarkLabel(innerLoopBody);
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Ldc_I4, 16);
            generator.Emit(OpCodes.Shr_Un);
            generator.Emit(OpCodes.Stloc, zeroes);

            // position += 1;
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldc_I4_2);
            generator.Emit(OpCodes.Conv_I);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stloc, positionLocal);

            // if (ushort)zeroes == 0, loop
            generator.MarkLabel(innerLoopCondition);
            generator.Emit(OpCodes.Ldloc, zeroes);
            generator.Emit(OpCodes.Conv_I2);
            generator.Emit(OpCodes.Brfalse, innerLoopBody);
            generator.Emit(OpCodes.Br, end);

            generator.MarkLabel(notFound);

            // position += 4;
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldc_I4_8);
            generator.Emit(OpCodes.Conv_I);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stloc, positionLocal);

            //// if position < endPosition, loop
            generator.MarkLabel(loopCondition);
            generator.Emit(OpCodes.Ldloc, positionLocal);
            generator.Emit(OpCodes.Ldloc, endLocal);
            generator.Emit(OpCodes.Clt_Un);
            generator.Emit(OpCodes.Brtrue, loopBody);

            generator.MarkLabel(end);
        }
    }
}
 
 