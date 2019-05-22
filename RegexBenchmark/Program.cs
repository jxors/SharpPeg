using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Optimizations;
using SharpPeg.Runner;
using SharpPeg.Runner.ILRunner;
using RegexToPeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpPeg.Runner.Interpreter;

namespace RegexBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileData = File.ReadAllText("mark.txt").Replace("\r\n", "\n").ToCharArray();
#if DEBUG
            var repeat = 5;
#else
            var repeat = 50;
#endif

            var patternCompilers = new[]
            {
                ("Optimized", new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EmitErrorInfo = false })),
                ("Interpreter", new PatternCompiler(new Compiler(), new DefaultOptimizer(), new InterpreterJitter())),
                ("Unoptimized",new PatternCompiler(new Compiler(), null, new ILJitter() { EmitErrorInfo = false })),
                ("Memoized",new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EmitErrorInfo = false })),
            };

            foreach(var (name, patternCompiler) in patternCompilers)
            {
                Console.WriteLine();
                Console.WriteLine($"================= {name} =================");
                Console.WriteLine();
                
                Benchmark(patternCompiler, "Twain", fileData, repeat);
                Benchmark(patternCompiler, "[a-z]shing", fileData, repeat);
                Benchmark(patternCompiler, "Huck[a-zA-Z]+|Saw[a-zA-Z]+", fileData, repeat);
                //Benchmark(patternCompiler, "[a-q][^u-z]{13}x", fileData, repeat);
                Benchmark(patternCompiler, "Tom|Sawyer|Huckleberry|Finn", fileData, repeat);
                Benchmark(patternCompiler, ".{0,2}(Tom|Sawyer|Huckleberry|Finn)", fileData, repeat);
                Benchmark(patternCompiler, ".{2,4}(Tom|Sawyer|Huckleberry|Finn)", fileData, repeat);
                Benchmark(patternCompiler, "Tom.{10,25}river|river.{10,25}Tom", fileData, repeat);
                Benchmark(patternCompiler, "[a-zA-Z]+ing", fileData, repeat);
                Benchmark(patternCompiler, "([A-Za-z]awyer|[A-Za-z]inn)\\s", fileData, repeat);
                Benchmark(patternCompiler, "\\s[a-zA-Z]{0,12}ing\\s", fileData, repeat);
                Benchmark(patternCompiler, "[\"'][^\"']{0,30}[?!\\.][\"']", fileData, repeat);

            }
        }
        
        static RegexGrammar g = new RegexGrammar(PatternCompiler.Default);

        static void Benchmark(PatternCompiler patternCompiler, string regex, char[] data, int repeat)
        {
            Console.WriteLine($"=== {regex} ===");
            var converter = new RegexConverter();
            var c = converter.Convert(g.ParseExpression(regex));

            // Console.WriteLine($"Translated as: {c}");
            var matchPattern = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, c), new Any()));

            var p = new Pattern(null)
            {
                Data = matchPattern
            };

            var runner = patternCompiler.Compile(p);
            var s = new Stopwatch();

            long found = 0, time = 0, best_time = 0;
            for (var i = 0; i < repeat; i++)
            {
                var captures = new List<Capture>(100000);
                s.Restart();
                runner.Run(data, 0, data.Length, captures);
                s.Stop();
                found = captures.Count;
                
                time = s.ElapsedMilliseconds;
                if (best_time == 0 || time < best_time)
                    best_time = time;
            }

            PrintResult("cpeg", best_time, found);
        }

        static void PrintResult(string name, long time, long numFound)
        {
            Console.WriteLine($"[{name.PadRight(8)}] time: {time.ToString().PadLeft(5)} ms ({numFound} matches)\n");
        }
    }
}
