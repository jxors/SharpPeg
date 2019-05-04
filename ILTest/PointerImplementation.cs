using SharpPeg.Runner.ILRunner;

namespace ILBenchmark
{
    public class PointerImplementation : BaseJittedRunner
    {
        protected unsafe override UnsafePatternResult RunInternal(char* ptr)
        {
            char* position = ptr;
            while (true)
            {
                if (position + 2 < this.dataEndPtr && *position == 't' && position[1] == 'h' && (uint)(position[2] - 'a') <= '\u0019')
                {
                    char* position_0 = position;
                    position += 3;
                    while (position < this.dataEndPtr && (uint)(*position - 'a') <= '\u0019')
                    {
                        position++;
                    }
                    this.captures.Add(new TemporaryCapture(0, 0, position_0, position));
                }
                else
                {
                    if (position >= this.dataEndPtr)
                    {
                        break;
                    }
                    position++;
                }
            }
            return new UnsafePatternResult(0, position);
        }
    }
}
