using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    public unsafe class MemoizationHelper
    {
        public int Count => items.Length;

        private char*[] items;

        private int startPosition;

        private char* basePosition = null;

        public char* this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                return items[(startPosition + index) % items.Length];
            }
            set
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                items[(startPosition + index) % items.Length] = value;
            }
        }

        public MemoizationHelper(int numItems)
        {
            items = new char*[numItems];
        }

        public void Memoize(char* position, char* result)
        {
            if(basePosition == null)
            {
                basePosition = position;
            }

            var offset = position - basePosition;

            if (offset >= 0)
            {
                while ((offset = position - basePosition) >= Count)
                {
                    PopFront();
                }

                this[(int)offset] = result;
            }
        }

        public bool HasMemoized(char* position)
        {
            return position > basePosition && (position - basePosition) < Count;
        }

        public char* GetMemoizationResult(char* position)
        {
            if(!HasMemoized(position))
            {
                throw new NotImplementedException();
            }

            return this[(int)(position - basePosition)];
        }

        public char* PopFront()
        {
            var result = this[0];
            this[0] = null;

            // Increase startPosition by 1, wrapping around if necessary.
            startPosition = (startPosition + 1) % items.Length;
            basePosition += 1;

            return result;
        }
    }
}
