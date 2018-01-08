using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public class ForwardTraceResult
    {
        public int Position { get; }
        public ushort? NeedsImmediateStoreOf { get; }

        public ForwardTraceResult(int position, ushort? needsImmediateStoreOf = null)
        {
            Position = position;
            NeedsImmediateStoreOf = needsImmediateStoreOf;
        }

        public override string ToString()
        {
            return $"{Position} (Needs store of: +{NeedsImmediateStoreOf})";
        }

        public override bool Equals(object obj)
        {
            var result = obj as ForwardTraceResult;
            return result != null &&
                   Position == result.Position &&
                   NeedsImmediateStoreOf == result.NeedsImmediateStoreOf;
        }

        public override int GetHashCode()
        {
            var hashCode = -654380193;
            hashCode = hashCode * -1521134295 + Position.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ushort?>.Default.GetHashCode(NeedsImmediateStoreOf);
            return hashCode;
        }
    }
}
