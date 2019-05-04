using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public class BoundsCheckEntry
    {
        public int MinBounds { get; }

        public int MaxBounds { get; }

        public bool CanChange => MinBounds > -1 || MaxBounds < int.MaxValue;

        public static BoundsCheckEntry Default = new BoundsCheckEntry(-1, int.MaxValue);

        private BoundsCheckEntry(int min, int max)
        {
            MinBounds = min;
            MaxBounds = max;
        }

        public BoundsCheckEntry Advance(int offset)
        {
            return new BoundsCheckEntry(MinBounds - offset, MaxBounds == int.MaxValue ? int.MaxValue : MaxBounds - offset);
        }

        public BoundsCheckEntry SetMin(int offset)
        {
            return new BoundsCheckEntry(Math.Max(MinBounds, offset), MaxBounds);
        }

        public BoundsCheckEntry SetMax(int offset)
        {
            return new BoundsCheckEntry(MinBounds, Math.Min(offset, MaxBounds));
        }

        public BoundsCheckEntry UnionWith(BoundsCheckEntry other)
        {
            if (other == null)
            {
                return this;
            }

            return new BoundsCheckEntry(Math.Min(other.MinBounds, MinBounds), Math.Max(other.MaxBounds, MaxBounds));
        }

        public override string ToString()
        {
            return $"{MinBounds} <= Bound <= {MaxBounds}";
        }

        public override bool Equals(object obj)
        {
            var entry = obj as BoundsCheckEntry;
            return entry != null &&
                   MinBounds == entry.MinBounds &&
                   MaxBounds == entry.MaxBounds;
        }

        public override int GetHashCode()
        {
            var hashCode = 1267421854;
            hashCode = hashCode * -1521134295 + MinBounds.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxBounds.GetHashCode();
            return hashCode;
        }

        public static bool operator !=(BoundsCheckEntry a, BoundsCheckEntry b)
        {
            return !(a == b);
        }

        public static bool operator==(BoundsCheckEntry a, BoundsCheckEntry b)
        {
            if(object.ReferenceEquals(a, b))
            {
                return true;
            }

            if(a is null || b is null)
            {
                return false;
            }

            return a.MinBounds == b.MinBounds && a.MaxBounds == b.MaxBounds;
        }
    }
}
