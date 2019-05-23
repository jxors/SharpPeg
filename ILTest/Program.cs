using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using RegexToPeg;
using SharpPeg.Operators;
using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Optimizations;
using SharpPeg.Runner.ILRunner;
using SharpPeg.Runner;
using System.IO;
using System.Runtime.InteropServices;

namespace ILTest
{
    class Program
    {
        private static Operator letters = new CharacterClass(new CharacterRange('a', 'z'));
        private static Operator capitals = new CharacterClass(new CharacterRange('A', 'Z'));
                 
        private static Operator capitalsAndNonCapitals = new CharacterClass(new CharacterRange('a', 'z'), new CharacterRange('A', 'Z'));

        static void Main(string[] args)
        {
            TestCompare();
            //BenchmarkStringSearch();
            //{
            //    var jitter = new CustomJitter("Regex.dll");
            //    var rg = new RegexGrammar(new PatternCompiler(new Compiler(), new DefaultOptimizer(), jitter));
            //    jitter.Save();

            //    rg.ParseExpression("abc");
            //}

            var patternCompiler = new PatternCompiler(new Compiler(), null, new ILJitter());
            var regexGrammar = new Lazy<RegexGrammar>(() => new RegexGrammar(patternCompiler));
            var converter = new RegexConverter();
            var helper = new PegHelper(patternCompiler);
            helper.EnsureExpressionBuilt();
            //CompileAndWritePatternToFile("PegExpression", helper.GetExpressionPattern());

            //var input = "AAA AAAas ntAar ".ToCharArray();
            var input = GenerateInputData(1 << 20);

            //var pattern = new PointerImplementation();
            //var patternStr = "([A-Za-z] 'awyer' [ \t] / [A-Za-z] 'inn' [ \t])";
            //var patternStr = "([A-Za-z] 'x')";
            //var patternStr = "([A-Za-z] 'awyer' [ \t] / [A-Za-z] 'inn' [ \t])";
            //var patternStr = "'Tom' / 'Finn' / 'Sawyer' / 'Huckleberry'";
            //var patternStr = "'Tom' / 'Sawyer' / 'Huckleberry' / 'Finn' ";
            //var patternStr = "[ -z][ -z]([ -z][ -z]('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn') / [ -z]('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn') / ('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn'))";
            //var patternStr = "[ -z][ -z]([ -z][ -z]('T' / 'S') / [ -z]('T' / 'Sawye' / 'Huck') / 'Huckleberry')";
            //var patternStr = "[ -z][ -z]([ -z][ -z]('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn') / [ -z]('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn') / ('Tom' / 'Sawyer' / 'Huckleberry' / 'Finn'))";
            //var patternStr = "[ -z][ -z]([ -z][ -z]('T' / 'S' / 'H') / [ -z]('T' / 'S' / 'H') / ('T' / 'S'))";
            //var patternStr = $"[ -{char.MaxValue}][ -z]([ -z][ -z]('T' / 'S') / [ -z]('T'))";
            //var patternStr = ".. ('T' / 'SS' / 'HHH' / 'FFFF')";
            //var patternStr = "('T' / 'SS' / 'HHH' / 'FFFF')";
            //var patternStr = ".. ('TT' / 'FFF')";
            //var patternStr = "'Twain'";
            //var patternStr = "[a-z] 'shing'";
            //var patternStr = "[a-z]+";
            //var patternStr = "('Huck'[a-zA-Z]+) / ('Saw'[a-zA-Z]+)";
            //var m = $"[{char.MinValue}-uz-{char.MaxValue}]";
            //var patternStr = $"[a-q]{m}{m}{m}{m}{m}{m}{m}{m}{m}{m}{m}{m}{m} 'x'";
            //var pattern = CompileAndWritePatternToFile("SimpleMatch", new Pattern("SimpleMatch") { Data = helper.ParseExpression("[a-z]*") });

            //var p = converter.Convert(regexGrammar.Value.ParseExpression("Twain"));
            //var p = converter.Convert(regexGrammar.Value.ParseExpression("river.{20,50}Tom|Tom.{20,50}river"));
            //var p = converter.Convert(regexGrammar.Value.ParseExpression("river.{10,25}Tom|Tom.{10,25}river"));
            //var a = new Pattern("A");
            //a.Data = new PrioritizedChoice(new Sequence(letters, a), new Empty());
            //var p = new Sequence(letters, a);
            //var p = new Sequence(new PrioritizedChoice('T', 'R'), "om");//Operator.EndingWithGreedy(capitalsAndNonCapitals, CharacterClass.String("ing"));
            //var ws = new Pattern { Data = new ZeroOrMore(new CharacterClass(' ')) };
            //var p1 = new Pattern { Data = new Sequence(ws, CharacterClass.String("abc")) };
            //var p2 = new Pattern { Data = new Sequence(ws, CharacterClass.String("xyz")) };
            //var p = new PrioritizedChoice(p1, p2);
            //var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, converter.Convert(regexGrammar.Value.ParseExpression("([A-Za-z]awyer|[A-Za-z]inn)\\s"))), new Any()));
            //var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, converter.Convert(regexGrammar.Value.ParseExpression("Twain"))), new Any()));
            //var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, converter.Convert(regexGrammar.Value.ParseExpression("[a-z]shing"))), new Any()));
            //var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, converter.Convert(regexGrammar.Value.ParseExpression("[a-zA-Z]+ing"))), new Any()));
            var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, converter.Convert(regexGrammar.Value.ParseExpression("Twain"))), new Any()));

            var s2 = new Stopwatch();
            s2.Start();

            var peg = new Pattern("SimpleMatch")
            {
                Data = p,
                //Data = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, p), new Any()))
            };
            var pattern = CompileAndWritePatternToFile("SimpleMatch", peg);
            Console.WriteLine($"Saved ({s2.ElapsedMilliseconds}ms)");

            var text = "Tom..Huckleberry  Finn         Tom  Tom  Huck\nFinn,";
            var capts = new List<Capture>();
            var runResult = pattern.Run(text, capts);
            if (runResult.IsSuccessful && runResult.InputPosition == text.Length)
            {
                Console.WriteLine($"Successful match on '{text}'");
            }

            //for (var n = 0; n < 10; n++)
            //{
            //    for (var x = 0; x < 25; x++)
            //    {
            //        //var pegGrammar = new PegGrammar(new ILInterpreterFactory());
            //        //pegGrammar.EnsureExpressionBuilt();

            //        //var expression = pegGrammar.ParseExpression("'th' [a-z]+");
            //        //var compiler = (new ILCompilerFactory()).Create(new Pattern
            //        //{
            //        //    Data = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, expression), new Any()))
            //        //});

            //        Stopwatch s = new Stopwatch();
            //        s.Start();
            //        var result = default(RunResult);
            //        var captures = new List<Capture>();

            //        for (var i = 0; i < 1000; i++)
            //        {
            //            captures = new List<Capture>();
            //            result = pattern.Run(input, 0, input.Length, captures);
            //            if (!result.IsSuccessful)
            //            {
            //                Console.WriteLine("Match fail");
            //            }
            //        }
            //        s.Stop();
            //        Console.WriteLine($"That took {s.ElapsedMilliseconds}ms ({captures.Count})");
            //    }
            //}

            Console.ReadKey();
        }

        private static char[] GenerateInputData(int length)
        {
            IEnumerable<char> Internal()
            {
                var r = new Random();
                var str = "abcdefghijklmnopqrstuvwxyz ";
                for (var i = 0; i < length; i++)
                {
                    if (r.Next(50) == 0)
                    {
                        yield return 't';
                        yield return 'h';
                    } else if (r.Next(5) == 0)
                    {
                        yield return ' ';
                    }
                    else
                    {
                        yield return str[r.Next(str.Length - 1)];
                    }
                }
            }

            return Internal().ToArray();
        }

        private static IRunner CompileAndWritePatternToFile(string fname, Pattern expression)
        {
            var compiler = new Compiler();
            var jitter = new CustomJitter(fname) { EmitErrorInfo = false };
            var compiledPattern = compiler.Compile(expression);
            var optimizedPattern = new DefaultOptimizer().Optimize(compiledPattern);

            var runner = jitter.Compile(optimizedPattern);
            jitter.Save();

            return runner;
        }

        static void BenchmarkStringSearch()
        {
            var data = File.ReadAllText("mark.txt").ToCharArray();
            var s = new Stopwatch();
            s.Restart();
            var f1 = FindNormal(data);
            s.Stop();
            Console.WriteLine($"Normal took {s.ElapsedMilliseconds}");

            s.Restart();
            var f2 = FindBoyerMoore(data);
            s.Stop();
            Console.WriteLine($"Boyer-Moore took {s.ElapsedMilliseconds}");

            s.Restart();
            var f3 = FindWithMask(data);
            s.Stop();
            Console.WriteLine($"Masked took {s.ElapsedMilliseconds}");
            Console.WriteLine($"Findings: {f1} vs {f2} vs {f3}");
            Console.ReadLine();
        }

        static int FindNormal(char[] data)
        {
            var found = 0;
            for (var i = 0; i < 25; i++)
            {
                var pos = 0;
                var end = data.Length - 5;
                while (pos < end)
                {
                    if (data[pos] == 'T'
                        && data[pos + 1] == 'w'
                        && data[pos + 2] == 'a'
                        && data[pos + 3] == 'i'
                        && data[pos + 4] == 'n')
                    {
                        found++;
                        pos += 5;
                    }
                    else
                    {
                        pos++;
                    }
                }
            }

            return found;
        }

        static int FindBoyerMoore(char[] data)
        {
            var found = 0;
            for (var i = 0; i < 25; i++)
            {
                var pos = 4;
                while (pos < data.Length)
                {
                    switch(data[pos])
                    {
                        case 'T':
                            pos += 4;
                            break;
                        case 'w':
                            pos += 3;
                            break;
                        case 'a':
                            pos += 2;
                            break;
                        case 'n':
                            if (data[pos - 4] == 'T'
                                && data[pos - 3] == 'w'
                                && data[pos - 2] == 'a'
                                && data[pos - 1] == 'i')
                            {
                                found++;
                            }

                            pos += 1;
                            break;
                        default:
                            pos += 1;
                            break;
                    }
                }
            }

            return found;
        }

        static unsafe int FindWithMask(char[] data)
        {
            var searchFor = 'T';
            var mask = searchFor | ((ulong)searchFor << 16) | ((ulong)searchFor << 32) | ((ulong)searchFor << 48);
            fixed (char* unsafeData = data)
            {
                var found = 0;
                for (var i = 0; i < 25; i++)
                {
                    var ptr = unsafeData;
                    var end = unsafeData + data.Length - 5;
                    while (ptr < end)
                    {
                        while (ptr < end)
                        {
                            var line = *(ulong*)ptr;
                            var x = ~(line ^ mask);
                            var t0 = (x & 0x7fff7fff7fff7fffLU) + 0x0001000100010001LU;
                            var t1 = (x & 0x8000800080008000LU);
                            var zeroes = t0 & t1;
                            if(zeroes != 0)
                            {
                                break;
                            }
                            else
                            {
                                ptr += 4;
                            }
                        }

                        if (*ptr == 'T'
                            && *(ptr + 1) == 'w'
                            && *(ptr + 2) == 'a'
                            && *(ptr + 3) == 'i'
                            && *(ptr + 4) == 'n')
                        {
                            found++;
                            ptr += 5;
                        }
                        else
                        {
                            ptr++;
                        }
                    }
                }

                return found;
            }
        }

        static void TestCompare()
        {
            for(var i = 0; i <= 0xff; i++)
            {
                for(var j = 0; j <= 0xff; j++)
                {
                    var expected = i > j;
                    var x0 = ((i >> 1) | 0x80) - (((j + 1) >> 1));

                    var q = ~(i ^ j);
                    var t0 = (q & 0x7f) + 0x01;
                    var t1 = (q & 0x80);
                    var x1 = t0 & t1;

                    var actual = ((x0 & ~x1) & 0x80) != 0;

                    if(expected != actual)
                    {
                        Console.WriteLine($"Doesn't work for {i} > {j}: expected {expected} but got {actual}");
                    }
                }
            }
        }
        /*
         * C++ version:
         * This doesn't work, because there's a massive P/Invoke overhead
        const __m256i first = _mm256_set1_epi16(toFind);

	    while(ptr < end)
	    {
		    const __m256i block_first = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(ptr));
		    const __m256i eq_first = _mm256_cmpeq_epi8(first, block_first);

		    uint32_t mask = _mm256_movemask_epi8(eq_first);

		    while (mask != 0)
		    {
			    if ((mask & 1) != 0)
			    {
				    return ptr;
			    }

			    ptr += 1;
			    mask >>= 1;
		    }

		    ptr += 8;
	    }

	    return end;

        [DllImport("../../../Release/SimdSearch.dll", EntryPoint = "findNextChar", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        static unsafe extern IntPtr FindNextChar(IntPtr ptr, IntPtr end, char toFind);

        static unsafe int FindWithAvx256(char[] data)
        {
            var searchFor = 'T';
            fixed (char* unsafeData = data)
            {
                var found = 0;
                for (var i = 0; i < 25; i++)
                {
                    var ptr = unsafeData;
                    var end = unsafeData + data.Length - 5;
                    while (ptr < end)
                    {
                        ptr = (char*)FindNextChar((IntPtr)ptr, (IntPtr)end, searchFor);

                        if (*ptr == 'T'
                            && *(ptr + 1) == 'w'
                            && *(ptr + 2) == 'a'
                            && *(ptr + 3) == 'i'
                            && *(ptr + 4) == 'n')
                        {
                            found++;
                            ptr += 5;
                        }
                        else
                        {
                            ptr++;
                        }
                    }
                }

                return found;
            }
        }
        */

#if DEBUG
        private unsafe static char Test(char* pointer)
        {
            return *(pointer + 5);
        }
#endif
    }
}
