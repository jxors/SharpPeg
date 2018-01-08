using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PegMatch
{
    unsafe class UtfFileContentLoader : IContentLoader
    {
        unsafe class PointerContainer
        {
            public char* data;
        }

        public string Name { get; }

        public UtfFileContentLoader(string filename)
        {
            Name = filename;
        }

        const int BatchSize = 1000;
        public List<ContentCharData> ReadAllCharsByLine()
        {
            var output = new List<ContentCharData>();
            var length = new FileInfo(Name).Length;
            using (var mmf = MemoryMappedFile.CreateFromFile(Name))
            {
                using (var accessor = mmf.CreateViewAccessor())
                {
                    try
                    {
                        if (length > int.MaxValue)
                        {
                            throw new NotImplementedException();
                        }

                        byte* ptr = (byte*)0;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                        byte* lineStart = ptr;
                        byte* iterator = ptr;
                        byte* dataEnd = ptr + length;
                        
                        var himagic = (uint)0x80808080;
                        var lomagic = (uint)0x01010101;
                        while (iterator < dataEnd)
                        {
                            var val = (*(uint*)iterator) ^ 0x0a0a0a0a;
                            if (((val - lomagic) & ~val & himagic) != 0)
                            {
                                if (*iterator == 0x0a)
                                {
                                    var lineEnd = iterator;
                                    var numBytes = (int)(lineEnd - lineStart);
                                    var chars = new char[Encoding.UTF8.GetMaxCharCount(numBytes)];

                                    fixed (char* charPtr = chars)
                                    {
                                        var len = Encoding.UTF8.GetChars(lineStart, numBytes, charPtr, chars.Length);
                                        output.Add(new ContentCharData(chars, len));
                                    }

                                    lineStart = lineEnd + 1;
                                }

                                iterator++;
                            }
                            else
                            {
                                iterator += 4;
                            }
                        }

                        if(lineStart != iterator)
                        {
                            var numBytes = (int)(iterator - lineStart);
                            var chars = new char[Encoding.UTF8.GetMaxCharCount(numBytes)];

                            fixed (char* charPtr = chars)
                            {
                                var len = Encoding.UTF8.GetChars(lineStart, numBytes, charPtr, chars.Length);
                                output.Add(new ContentCharData(chars, len));
                            }
                        }
                    }
                    finally
                    {
                        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                }
            }

            return output;
        }
        
        public ContentCharData ReadAllChars()
        {
            var length = new FileInfo(Name).Length;
            using (var mmf = MemoryMappedFile.CreateFromFile(Name))
            {
                using (var view = mmf.CreateViewAccessor())
                {
                    return ReadFileDataUnsafe(view, length);
                }
            }
        }

        /// <summary>
        /// Method to quickly load a file into a char[] array.
        /// 
        /// Is about ~4x as fast as StreamReader.ReadToEnd
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private unsafe ContentCharData ReadFileDataUnsafe(MemoryMappedViewAccessor accessor, long length)
        {
            try
            {
                if(length > int.MaxValue)
                {
                    throw new NotImplementedException();
                }

                byte* ptr = (byte*)0;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                var splitIndexes = CalculateSplitIndexes(ptr, length);
                return LoadToOneBuffer(ptr, splitIndexes, length);
            }
            finally
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
        
        private unsafe ContentCharData LoadToOneBuffer(byte* ptr, List<long> splitIndexes, long length)
        {
            var maxCount = Encoding.UTF8.GetMaxCharCount((int)length);
            var container = new PointerContainer();
            var result = new char[maxCount];
            fixed (char* charPtr = result)
            {
                container.data = charPtr;
            }
            
            // Note: loading into multiple char[] arrays and combining them affterwards takes +50% more time.

            // Do the character count sequentially. Doing multiple reads at the same time slows down cold access
            // to a file. This does slow down access to cached files by about 25%.
            var charSizes = new int[splitIndexes.Count];
            for(var i = 0; i <  splitIndexes.Count - 1; i++)
            {
                var startPos = splitIndexes[i];
                var endPos = splitIndexes[i + 1];

                charSizes[i] = Encoding.UTF8.GetCharCount(ptr + startPos, (int)(endPos - startPos));
            }

            // Doing this in parallel obviously assumes that the entire file will be cached in memory.
            // But since we're loading everything into a char[] array anyways that doesn't seem like an
            // unreasonable assumption.

            // Even if this does not run in parallel, we still get the speedup from the reduced copying.
            // Only one copy is ever made, namely from byte* -> char[].
            var dataLength = 0;
            Parallel.For(0, splitIndexes.Count - 1, (i) =>
            {
                var startPos = (int)splitIndexes[i];
                var endPos = (int)splitIndexes[i + 1];
                var charOffset = charSizes.Take(i).Sum();

                var len = Encoding.UTF8.GetChars(ptr + startPos, endPos - startPos, container.data + charOffset, result.Length - charOffset);

                Interlocked.Add(ref dataLength, len);
            });

            return new ContentCharData(result, dataLength);
        }

        private unsafe List<long> CalculateSplitIndexes(byte* ptr, long length)
        {
            // 1 thread per 5 mb or 1 thread per core, whichever is lower
            var numSplits = (int)Math.Max(1, Math.Min(Environment.ProcessorCount, length / (5 << 20)));
            var blockSize = length / (numSplits + 2);
            var splitIndexes = new List<long>
            {
                0
            };
            splitIndexes.AddRange(Enumerable.Range(1, numSplits + 1).Select(item => FindSplitIndex(ptr, item * blockSize)));
            splitIndexes.Add(length);

            return splitIndexes;
        }

        /// <summary>
        /// Returns an index near the provided value such that 
        /// data[0]....data[index - 1] is one valid UTF-8 character array, and
        /// data[index] ... data[len - 1] is another valid UTF-8 character array.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private unsafe long FindSplitIndex(byte* data, long index)
        {
            // Inside a continuation
            if ((*(data + index) & 0b110_0000) != 0b1000_0000)
            {
                return index;
            }

            // Should be tail call optimized
            return FindSplitIndex(data, index - 1);
        }
    }
}
