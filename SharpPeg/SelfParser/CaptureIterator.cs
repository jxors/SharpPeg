using System;
using System.Collections.Generic;
using System.Text;
using SharpPeg.Runner;

namespace SharpPeg.SelfParser
{
    public class CaptureIterator<T>
    {
        private class CaptureEntry
        {
            public int StartIndex { get; }
            public int EndIndex { get; }
            public int OpenIndex { get; }

            public T Data { get; }
            public int CloseIndex { get; }

            public CaptureEntry(Capture capture, T data)
            {
                this.StartIndex = capture.StartPosition;
                this.EndIndex = capture.EndPosition;
                this.OpenIndex = capture.OpenIndex;
                CloseIndex = capture.BasePosition;
                this.Data = data;
            }
        }

        private readonly IReadOnlyList<Capture> m_Captures;
        private string m_InputData;

        public CaptureIterator(string inputData, IReadOnlyList<Capture> captures)
        {
            this.m_Captures = captures;
            this.m_InputData = inputData;
        }

        public IEnumerable<T> Iterate(Func<int, string, IReadOnlyList<T>, T> build)
        {
            return Iterate((a, b, _, c) => build(a, b, c));
        }

        public IEnumerable<T> Iterate(Func<int, string, Capture, IReadOnlyList<T>, T> build)
        {
            if (m_Captures.Count > 0)
            {
                var capturedObjects = new Stack<CaptureEntry>();
                for (var i = m_Captures.Count - 1; i >= 0; i--)
                {
                    var capture = m_Captures[i];
                    var strData = m_InputData.Substring(capture.StartPosition, capture.EndPosition - capture.StartPosition);

                    var parameters = new List<T>();

                    while (capturedObjects.Count > 0)
                    {
                        var top = capturedObjects.Peek();
                        if (top.StartIndex >= capture.StartPosition && top.EndIndex <= capture.EndPosition && top.OpenIndex >= capture.OpenIndex && top.CloseIndex <= capture.BasePosition)
                        {
                            parameters.Add(capturedObjects.Pop().Data);
                        }
                        else
                        {
                            break;
                        }
                    }

                    capturedObjects.Push(new CaptureEntry(capture, build(capture.Key, strData, capture, parameters)));
                }

                while (capturedObjects.Count > 0)
                {
                    yield return capturedObjects.Pop().Data;
                }
            }
        }
    }
}
