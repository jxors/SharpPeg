namespace SharpPeg.Optimizations.Default.Analyzers
{
    public abstract class NdStateView
    {
        public abstract int Position { get; }

        public abstract int Advances { get; }

        public abstract int[] Vars { get; }

        public abstract NdState AdvanceAndMoveOneForward(int offset);
        public abstract NdState Move(int offset);
        public abstract NdState SetMaxBounds(int offset);
        public abstract NdState SetMinBounds(int offset);
        public abstract NdState StoreAndMoveOneForward(ushort var);
        public abstract NdState WithAdvancesAndMoveOneForward(int newValue);
        public abstract NdState WithPosition(int position);
    }
}