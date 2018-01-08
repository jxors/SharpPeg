using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class CaptureKeyAllocator
    {
        private List<string> mapping = new List<string>() { "" };

        private HashSet<string> alreadyMapped = new HashSet<string>() { "" };

        public int this[string key]
        {
            get
            {
                if (alreadyMapped.Contains(key))
                {
                    return mapping.IndexOf(key);
                }
                else
                {
                    alreadyMapped.Add(key);

                    var result = mapping.Count;
                    mapping.Add(key);
                    return result;
                }
            }
        }

        public string this[int key] => mapping[key];
    }
}
