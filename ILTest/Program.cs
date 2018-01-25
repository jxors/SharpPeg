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

namespace ILTest
{
    class Program
    {
        private static Operator letters = new CharacterClass(new CharacterRange('a', 'z'));
        private static Operator capitals = new CharacterClass(new CharacterRange('A', 'Z'));
                 
        private static Operator capitalsAndNonCapitals = new CharacterClass(new CharacterRange('a', 'z'), new CharacterRange('A', 'Z'));

        static void Main(string[] args)
        {
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
            var p = new Sequence(new PrioritizedChoice('T', 'R'), "om");//Operator.EndingWithGreedy(capitalsAndNonCapitals, CharacterClass.String("ing"));
            //var p = helper.ParseExpression(patternStr);

            var s2 = new Stopwatch();
            s2.Start();

            var peg = new Pattern("SimpleMatch")
            {
                Data = p,
                //Data = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, p), new Any()))
            };
            var pattern = CompileAndWritePatternToFile("SimpleMatch", peg);
            Console.WriteLine($"Saved ({s2.ElapsedMilliseconds}ms)");

            var text = "abHuckleberry sntasoretneirtneisrnteisrnietaneristniesra river Tom Tom river";
            var capts = new List<Capture>();
            var runResult = pattern.Run(text, capts);
            if (runResult.IsSuccessful && runResult.InputPosition == text.Length)
            {
                Console.WriteLine($"Successful match on '{text}'");
            }

            for (var i = 0; i < 1000; i++)
            {
                //new SharpPeg.Runner.ILCompiler.Compiler(peg).Compile();
            }

            for (var n = 0; n < 100; n++)
            {
                for (var x = 0; x < 25; x++)
                {
                    //var pegGrammar = new PegGrammar(new ILInterpreterFactory());
                    //pegGrammar.EnsureExpressionBuilt();

                    //var expression = pegGrammar.ParseExpression("'th' [a-z]+");
                    //var compiler = (new ILCompilerFactory()).Create(new Pattern
                    //{
                    //    Data = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, expression), new Any()))
                    //});

                    Stopwatch s = new Stopwatch();
                    s.Start();
                    var result = default(RunResult);
                    var captures = new List<Capture>();

                    for (var i = 0; i < 1000; i++)
                    {
                        captures = new List<Capture>();
                        result = pattern.Run(input, 0, input.Length, captures);
                        if (!result.IsSuccessful)
                        {
                            Console.WriteLine("Match fail");
                        }
                    }
                    s.Stop();
                    Console.WriteLine($"That took {s.ElapsedMilliseconds}ms ({captures.Count})");
                }
            }

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

#if DEBUG
        private unsafe static char Test(char* pointer)
        {
            return *(pointer + 5);
        }
#endif
    }
}
