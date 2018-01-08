using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class CharacterClass : Operator
    {
        public IEnumerable<char> Value
        {
            get
            {
                foreach(var range in ranges)
                {
                    for(var c = range.Min; c <= range.Max; c++)
                    {
                        yield return c;
                    }
                }
            }
        }

        public IReadOnlyList<CharacterRange> Ranges => ranges;

        public override IEnumerable<Operator> Children => Enumerable.Empty<Operator>();

        public int NumChars => ranges.Sum(item => item.Max - item.Min + 1);

        private List<CharacterRange> ranges;

        public CharacterClass(char value) : this(new[] { value }) { }

        public CharacterClass(IEnumerable<char> chars) : this(BuildRanges(chars)) { }

        public CharacterClass(params char[] chars) : this(BuildRanges(chars)) { }

        public CharacterClass(params CharacterRange[] ranges) : this((IEnumerable<CharacterRange>)ranges) { }
        public CharacterClass(IEnumerable<(char min, char max)> ranges, params char[] chars) : this((IEnumerable<CharacterRange>)ranges.Select(item => new CharacterRange(item.min, item.max)).Concat(chars.Select(c => new CharacterRange(c, c)))) { }
        public CharacterClass(IEnumerable<CharacterRange> ranges, params char[] chars) : this((IEnumerable<CharacterRange>)ranges.Concat(chars.Select(c => new CharacterRange(c, c)))) { }

        public CharacterClass(IEnumerable<CharacterRange> ranges)
        {
            this.ranges = ranges.ToList();

            if(this.ranges.Count <= 0)
            {
                throw new ArgumentException("CharacterSet must contain at least one character");
            }
        }

        private static IEnumerable<CharacterRange> BuildRanges(IEnumerable<char> values)
        {
            var intervals = new Dictionary<int, int>();
            var seen = new HashSet<char>();
            foreach (var c in values)
            {
                if (!seen.Contains(c))
                {
                    seen.Add(c);
                    var high = (int)c;
                    var low = (int)c;

                    if (intervals.TryGetValue(c + 1, out int foundHigh))
                    {
                        high = foundHigh;
                    }

                    if (intervals.TryGetValue(c - 1, out int foundLow))
                    {
                        low = foundLow;
                    }

                    intervals[high] = low;
                    intervals[low] = high;
                }
            }

            foreach (var item in intervals)
            {
                if (item.Key <= item.Value)
                {
                    yield return new CharacterRange((char)item.Key, (char)item.Value);
                }
            }
        }

        public static Operator String(string str)
        {
            if (str.Length <= 0)
            {
                return new Empty();
            }
            else if (str.Length <= 1)
            {
                return new CharacterClass(str[0]);
            }
            else
            {
                return new Sequence(str.Select(c => new CharacterClass(c)));
            }
        }

        public static Operator Range(char min, char max)
        {
            return new CharacterClass(new CharacterRange(min, max));
        }

        public static Operator All()
        {
            return Range(char.MinValue, char.MaxValue);
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new CharacterClass(ranges);

        public override string ToString()
        {
            if (ranges.Count == 1 && ranges.First().Size == 1)
            {
                return $"'{Value.First()}'";
            }
            else
            {
                return "[" + string.Join("", ranges.Select(item => item.Size == 1 ? $"{HumanReadable(item.Min)}" : $"{HumanReadable(item.Min)}-{HumanReadable(item.Max)}")) + "]";
            }
        }

        private string HumanReadable(char c)
        {
            if(c < 32 || c >= 0xffff)
            {
                return $"\\u{(int)c:X4}";
            }
            else
            {
                return $"{c}";
            }
        }
    }
}
