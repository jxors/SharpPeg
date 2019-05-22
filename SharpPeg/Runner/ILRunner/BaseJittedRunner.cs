using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpPeg.Operators;
using System.Linq;
using SharpPeg.Common;
using System.Runtime.CompilerServices;
#if NET_CORE_30
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace SharpPeg.Runner.ILRunner
{
    public unsafe abstract class BaseJittedRunner : IRunner
    {
        public int dataSize;
        public char[] data;
        public List<TemporaryCapture> captures;
        public int captureCloseIndex;
        public char* dataPtr;
        public char* dataEndPtr;
        public char*[] EntryPoints, ExitPoints;
        public List<Method> Methods;

        public Dictionary<(int pattern, int position), (int baseIndex, List<TemporaryCapture> list)> memo;
        protected void ApplyMemoizedResult(int patternId, int position)
        {
            var data = memo[(patternId, position)];
            var openIndex = captures.Count;
            foreach(var item in data.list)
            {
                captures.Add(new TemporaryCapture(item.CaptureKey, openIndex + (item.OpenIndex - data.baseIndex), item.StartIndex, item.EndIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char* CharScanNormal(char* startPos, char* endPos, char searchFor)
        {
            if (startPos > endPos)
            {
                return startPos;
            }

#if NET_CORE_30
            if (Avx.IsSupported)
            {
                var pos = startPos;
                var mask = Vector256.Create(searchFor);
                while (pos < endPos)
                {
                    var other = Avx2.LoadVector256((byte*)pos).As<byte, ushort>();
                    var result = Avx2.CompareEqual(mask, other);
                    var resultMask = Avx2.MoveMask(result.As<ushort, byte>());
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
            }
            else
#endif
            {
                var mask = ~(searchFor | ((ulong)searchFor << 16) | ((ulong)searchFor << 32) | ((ulong)searchFor << 48));
                var pos = startPos;
                while (pos < endPos)
                {
                    var line = *(ulong*)pos;
                    var x = (line ^ mask);
                    var t0 = (x & 0x7fff7fff7fff7fffLU) + 0x0001000100010001LU;
                    var t1 = (x & 0x8000800080008000LU);
                    var zeroes = t0 & t1;
                    if (zeroes != 0)
                    {
                        while ((ushort)zeroes == 0) { pos++; zeroes >>= 16; }
                        return pos;
                    }
                    else
                    {
                        pos += 4;
                    }
                }
            }

            return endPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char* CharScan2Normal(char* startPos, char* endPos, char searchFor1, char searchFor2)
        {
            if (startPos > endPos)
            {
                return startPos;
            }


#if NET_CORE_30
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
            }
            else
#endif
            {
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
            }

            return endPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char* CharScan3Normal(char* startPos, char* endPos, char searchFor1, char searchFor2, char searchFor3)
        {
            if (startPos > endPos)
            {
                return startPos;
            }


#if NET_CORE_30
            if (Avx.IsSupported)
            {
                var pos = startPos;
                var mask1 = Vector256.Create(searchFor1);
                var mask2 = Vector256.Create(searchFor2);
                var mask3 = Vector256.Create(searchFor3);
                while (pos < endPos)
                {
                    var other = Avx2.LoadVector256((byte*)pos).As<byte, ushort>();
                    var result1 = Avx2.CompareEqual(mask1, other);
                    var resultMask1 = Avx2.MoveMask(result1.As<ushort, byte>());
                    var result2 = Avx2.CompareEqual(mask2, other);
                    var resultMask2 = Avx2.MoveMask(result2.As<ushort, byte>());
                    var result3 = Avx2.CompareEqual(mask3, other);
                    var resultMask3 = Avx2.MoveMask(result3.As<ushort, byte>());
                    var resultMask = resultMask1 | resultMask2 | resultMask3;
                    if (resultMask != 0)
                    {
                        while ((resultMask & 1) == 0) { pos++; resultMask >>= 1; }
                        return pos;
                    }
                    else
                    {
                        pos += 16;
                    }
                }
            }
            else
#endif
            {
                var mask1 = ~(searchFor1 | ((ulong)searchFor1 << 16) | ((ulong)searchFor1 << 32) | ((ulong)searchFor1 << 48));
                var mask2 = ~(searchFor2 | ((ulong)searchFor2 << 16) | ((ulong)searchFor2 << 32) | ((ulong)searchFor2 << 48));
                var mask3 = ~(searchFor3 | ((ulong)searchFor3 << 16) | ((ulong)searchFor3 << 32) | ((ulong)searchFor3 << 48));
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
                            return pos;
                        }
                    }

                    {
                        var x = (line ^ mask3);
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
            }

            return endPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char* CharScan4Normal(char* startPos, char* endPos, char searchFor1, char searchFor2, char searchFor3, char searchFor4)
        {
            if (startPos > endPos)
            {
                return startPos;
            }


#if NET_CORE_30
            if (Avx.IsSupported)
            {
                var pos = startPos;
                var mask1 = Vector256.Create(searchFor1);
                var mask2 = Vector256.Create(searchFor2);
                var mask3 = Vector256.Create(searchFor3);
                var mask4 = Vector256.Create(searchFor4);
                while (pos < endPos)
                {
                    var other = Avx2.LoadVector256((byte*)pos).As<byte, ushort>();
                    var result1 = Avx2.CompareEqual(mask1, other);
                    var resultMask1 = Avx2.MoveMask(result1.As<ushort, byte>());
                    var result2 = Avx2.CompareEqual(mask2, other);
                    var resultMask2 = Avx2.MoveMask(result2.As<ushort, byte>());
                    var result3 = Avx2.CompareEqual(mask3, other);
                    var resultMask3 = Avx2.MoveMask(result3.As<ushort, byte>());
                    var result4 = Avx2.CompareEqual(mask4, other);
                    var resultMask4 = Avx2.MoveMask(result4.As<ushort, byte>());
                    var resultMask = resultMask1 | resultMask2 | resultMask3 | resultMask4;
                    if (resultMask != 0)
                    {
                        while ((resultMask & 1) == 0) { pos++; resultMask >>= 1; }
                        return pos;
                    }
                    else
                    {
                        pos += 16;
                    }
                }
            }
            else
#endif
            {
                var mask1 = ~(searchFor1 | ((ulong)searchFor1 << 16) | ((ulong)searchFor1 << 32) | ((ulong)searchFor1 << 48));
                var mask2 = ~(searchFor2 | ((ulong)searchFor2 << 16) | ((ulong)searchFor2 << 32) | ((ulong)searchFor2 << 48));
                var mask3 = ~(searchFor3 | ((ulong)searchFor3 << 16) | ((ulong)searchFor3 << 32) | ((ulong)searchFor3 << 48));
                var mask4 = ~(searchFor4 | ((ulong)searchFor4 << 16) | ((ulong)searchFor4 << 32) | ((ulong)searchFor4 << 48));
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

                    {
                        var x = (line ^ mask3);
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
                        var x = (line ^ mask4);
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
            }

            return endPos;
        }

        protected void Memoize(int patternId, int startPosition, int startCaptures)
        {
            if(memo == null)
            {
                memo = new Dictionary<(int pattern, int position), (int baseIndex, List<TemporaryCapture>)>();
            }

            var newList = new List<TemporaryCapture>(captures.Count - startCaptures);
            var minOpenIndex = int.MaxValue;
            for(var i = startCaptures; i < captures.Count; i++)
            {
                var capture = captures[i];
                newList.Add(capture);
                if(captures[i].OpenIndex < minOpenIndex)
                {
                    minOpenIndex = captures[i].OpenIndex;
                }
            }

            memo[(patternId, startPosition)] = (newList.Count > 0 ? minOpenIndex : 0, newList);
        }

        private class StackTraceReconstruction : IComparable<StackTraceReconstruction>
        {
            public Method Method { get; }

            public int StartPos { get; }

            public int EndPos { get; }

            public bool WasCompleted { get; }

            public StackTraceReconstruction(Method method, int startPos, int endPos, bool successful)
            {
                Method = method;
                StartPos = startPos;
                EndPos = endPos;
                WasCompleted = successful;
            }

            public int CompareTo(StackTraceReconstruction other)
            {
                // Sort ascending on start position
                return StartPos - other.StartPos;
            }

            public override string ToString()
            {
                return $"{Method.Name ?? "[unnamed pattern]"} at position {StartPos}" + (WasCompleted ? $"..{EndPos} (completed)" : "");
            }
        }

        public IEnumerable<string> GetPatternsTriedAt(int index)
        {
            var list = new List<string>();
            var pos = dataPtr + index;
            for (var i = 0; i < EntryPoints.Length; i++)
            {
                if (EntryPoints[i] == pos)
                {
                    list.Add(Methods[i].Name);
                }
            }

            return memo.Keys
                .Where(k => k.position == index)
                .Select(k => Methods[k.pattern].Name);
        }

        public IEnumerable<string> GetPatternsFinishedAt(int index)
        {
            var list = new List<string>();
            var pos = dataPtr + index;
            for (var i = 0; i < EntryPoints.Length; i++)
            {
                if (ExitPoints[i] == pos)
                {
                    list.Add(Methods[i].Name);
                }
            }

            return list;
        }

        public string ExplainResult(RunResult result, string inputData)
        {
            if(result.IsSuccessful)
            {
                return $"Parsing was successful, parsing {result.InputPosition} out of {inputData.Length} characters.";
            }

            if(EntryPoints == null || ExitPoints == null)
            {
                throw new NotSupportedException("Runner was not compiled with error reporting information");
            }

            var items = new List<StackTraceReconstruction>();
            for (var i = 0; i < EntryPoints.Length; i++)
            {
                if (EntryPoints[i] != null)
                {
                    items.Add(new StackTraceReconstruction(Methods[i], (int)(EntryPoints[i] - dataPtr), ExitPoints[i] == null ? -1 : (int)(EntryPoints[i] - dataPtr), EntryPoints[i] < ExitPoints[i]));
                }
            }

            items.Sort();

            var completedBegin = 0;
            var completedEnd = 0;
            for(var i = 0; i < items.Count - 1; i++)
            {
                if(!items[i].WasCompleted && items[i + 1].WasCompleted)
                {
                    completedBegin = i + 1;
                } else if (items[i].WasCompleted && !items[i + 1].WasCompleted)
                {
                    completedEnd = i;
                }
            }

            var errorBuilder = new StringBuilder();
            var errorStartPos = items[completedBegin].StartPos;
            var errorEndPos = items[completedEnd].EndPos + 1;
            errorBuilder.Append($"Parsing failed trying to parse ")
                .Append(string.Join(" => ", items.Take(completedBegin)))
                .AppendLine(".")
                .Append("Expected one of the following: ")
                .Append(string.Join(", ", items.Skip(completedEnd + 1).Select(item => item.Method.Name ?? "[unnamed pattern]").OrderBy(item => item.Length)))
                .Append(" at position ")
                .Append(items.Last().StartPos)
                .Append(" after '")
                .Append(inputData.Substring(errorStartPos, errorEndPos - errorStartPos))
                .Append("' (starting here: '")
                .Append(inputData.Substring(errorEndPos, Math.Min(inputData.Length - errorEndPos, 10)))
                .Append("').")
            ;

            var errorStr = errorBuilder.ToString();
            return errorStr;
        }

        protected void DiscardCaptures(int newCount)
        {
            if (newCount < captures.Count)
            {
                captures.RemoveRange(newCount, captures.Count - newCount);
            }
        }

        protected abstract unsafe UnsafePatternResult RunInternal(char* position);

        public RunResult Run(char[] inputData, int index, int length, List<Capture> captureOutput = null)
        {
            if(inputData.Length <= 0)
            {
                // fixed pointer to an empty array returns null
                inputData = new char[] { '\0' };
            }

            data = inputData;
            fixed (char* dptr = data)
            {
                dataPtr = dptr;
                dataEndPtr = dataPtr + index + length;
                if (index > inputData.Length)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                if (index + length > inputData.Length)
                {
                    throw new IndexOutOfRangeException(nameof(length));
                }

                dataSize = index + length;
                captures = new List<TemporaryCapture>();
                captureCloseIndex = 0;

                var result = RunInternal(dataPtr + index);
                if (captureOutput != null)
                {
                    var needsSorting = false;
                    for (var i = 0; i < captures.Count; i++)
                    {
                        var capture = captures[i];
                        captureOutput.Add(new Capture(capture.CaptureKey, (int)(capture.StartIndex - dataPtr), (int)(capture.EndIndex - dataPtr), capture.OpenIndex, i));
                        if(captureOutput.Count >= 2 && captureOutput[captureOutput.Count - 2].CompareTo(captureOutput[captureOutput.Count - 1]) > 0)
                        {
                            needsSorting = true;
                        }
                    }

                    if (needsSorting)
                    {
                        captureOutput.Sort();
                    }
                }

                var position = (int)(result.Position - dataPtr);
                return new RunResult(result.Label == 0, result.Label, position, 0);
            }
        }

        public RunResult Run(string stringData, List<Capture> captureOutput = null) => Run(stringData.ToCharArray(), 0, stringData.Length, captureOutput);
    }
}
