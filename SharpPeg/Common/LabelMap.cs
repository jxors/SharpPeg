using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Common
{
    public class LabelMap
    {
        public const int DEFAULT_FAIL_LABEL = 1;

        private List<(int failureLabel, ushort jumpTarget)> mapping;

        public IReadOnlyList<(int failureLabel, ushort jumpTarget)> Mapping => mapping;

        public LabelMap(IEnumerable<(int, ushort)> mapping)
        {
            this.mapping = mapping.ToList();
        }
    }
}
