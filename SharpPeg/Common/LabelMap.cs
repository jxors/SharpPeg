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

        public bool TryGet(int index, out ushort result)
        {
            foreach(var (failureLabel, jumpTarget) in mapping)
            {
                if(failureLabel == index)
                {
                    result = jumpTarget;
                    return true;
                }
            }

            result = 0;
            return false;
        }
    }
}
