using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Common
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        public int Count { get; private set; } = 0;

        private T[] items;

        private int startPosition;

        public T this[int index]
        {
            get
            {
                if(index < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                if(index >= Count)
                {
                    return default(T);
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

        public CircularBuffer(int numItems)
        {
            items = new T[numItems];
        }

        public CircularBuffer() : this(8)
        { }

        public CircularBuffer(T[] items, int startPosition, int count)
        {
            this.items = items;
            this.startPosition = startPosition;
            Count = count;
        }

        private void EnsureSize(int numNew)
        {
            if (Count + numNew > items.Length)
            {
                var oldSize = items.Length;
                Array.Resize(ref items, oldSize * 2);

                if (startPosition + Count > oldSize)
                {
                    var itemsAtEnd = (oldSize - startPosition) % oldSize;
                    for (var i = 1; i <= itemsAtEnd; i++)
                    {
                        items[items.Length - i] = items[oldSize - i];
                        items[oldSize - i] = default(T);
                    }

                    startPosition = items.Length - itemsAtEnd;
                }
            }
        }

        public void PushFront(T item)
        {
            EnsureSize(1);

            // Decrease startPosition by 1, wrapping around if necessary.
            startPosition = (startPosition + (items.Length - 1)) % items.Length;
            Count += 1;
            this[0] = item;
        }

        public void PushBack(T item)
        {
            EnsureSize(1);

            Count += 1;
            this[Count - 1] = item;
        }

        public T PopFront()
        {
            var result = this[0];
            this[0] = default(T);

            // Increase startPosition by 1, wrapping around if necessary.
            startPosition = (startPosition + 1) % items.Length;
            Count -= 1;

            return result;
        }

        public T PopBack()
        {
            var result = this[Count - 1];
            this[Count - 1] = default(T);
            
            Count -= 1;

            return result;
        }

        public CircularBuffer<T> Clone()
        {
            return new CircularBuffer<T>(items.ToArray(), startPosition, Count);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for(var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
