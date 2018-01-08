using Newtonsoft.Json.Linq;
using PegMatch.Grammar;
using SharpPeg.Runner;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public class CapturesToNodesConverter
    {
        public string InputData { get; }
        public CaptureKeyAllocator CaptureKeyAllocator { get; }

        public IReadOnlyList<Capture> Captures { get; }

        public CapturesToNodesConverter(string inputData, CaptureKeyAllocator captureKeyAllocator, IEnumerable<Capture> captures)
        {
            InputData = inputData;
            CaptureKeyAllocator = captureKeyAllocator;
            Captures = captures.ToList();
        }

        public Node ToNodes()
        {
            var iterator = new CaptureIterator<Node>(InputData, Captures);
            var result = iterator.Iterate(BuildNode).ToList();

            if(result.Count == 1)
            {
                return result[0];
            }
            else
            {
                return new ListNode("matches", result);
            }
        }

        private Node BuildNode(int key, string captureData, IReadOnlyList<Node> parameters)
        {
            if (key == 0) // Base capture
            {
                if (parameters.Count <= 0)
                {
                    return new ScalarNode("match", captureData);
                }

                return new ObjectNode("match", parameters);
            }
            else
            {
                var name = CaptureKeyAllocator[key];
                if (parameters.Count <= 0)
                {
                    return new ScalarNode(name, captureData);
                }

                if(name.EndsWith("[]"))
                {
                    return new ListNode(name.Substring(0, name.Length - 2), parameters);
                }

                return new ObjectNode(name, parameters);
            }
        }
    }
}
