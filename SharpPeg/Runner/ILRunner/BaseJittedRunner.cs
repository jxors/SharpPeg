using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpPeg.Operators;
using System.Linq;
using SharpPeg.Common;

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

        protected abstract unsafe char* RunInternal(char* position);

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

                if (result == null)
                {
                    return new RunResult(false, 0, 0);
                }
                else
                {
                    return new RunResult(true, (int)(result - dataPtr), 0);
                }
            }
        }

        public RunResult Run(string stringData, List<Capture> captureOutput = null) => Run(stringData.ToCharArray(), 0, stringData.Length, captureOutput);
    }
}
