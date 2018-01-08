using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpPeg.Operators;
using SharpPeg.Runner;
using SharpPeg.SelfParser;
using PegMatch.Grammar;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using PegMatch.OutputProcessing;
using Newtonsoft.Json;
using SharpPeg.Compilation;
using SharpPeg;

namespace PegMatch
{
    class Program
    {
        static int Main(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            var pattern = commandLineApplication.Argument("pattern", "The PEG to match.");

            var files = commandLineApplication.Argument("files", "Files to match.", multipleValues: true);

            var outputJson = commandLineApplication.Option(
                "-j |--output-json",
                "Output matches as structured JSON data. Captures can be used to split data into separate JSON fields. Like this: {name: expression}",
                CommandOptionType.NoValue);

            var libraryDirs = commandLineApplication.Option("-l |--libraries", "Additional PEG library paths.", CommandOptionType.MultipleValue);

            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.OnExecute(() =>
            {
                if(pattern?.Value == null)
                {
                    Console.WriteLine("Missing pattern.");
                    return -1;
                }

                try
                {
                    var captureKeyAllocator = outputJson.HasValue() ? new CaptureKeyAllocator() : null;

                    var loader = new PegLoader(CreateLibraryPaths(libraryDirs), captureKeyAllocator);
                    var inputFiles = LoadInputAsync(files.Values).ToArray();

                    // TODO: Check whether the interpreter is faster for parsing the initial pattern
                    var expression = loader.Parse(pattern.Value);
                    var runner = BuildRunnerFromExpression(PatternCompiler.Default, expression);

                    var settings = new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented,
                    };

                    foreach (var (fileName, data) in inputFiles)
                    {
                        var text = data.GetAwaiter().GetResult();

                        var captures = new List<Capture>();
                        if (runner.Run(text.Data, 0, text.Length, captures).IsSuccessful)
                        {
                            if (outputJson.HasValue())
                            {
                                var conv = new CapturesToNodesConverter(new string(text.Data), captureKeyAllocator, captures);
                                var json = new NodeToJsonConverter(conv.ToNodes(), settings);
                                Console.WriteLine(json.ToJson());
                            }
                            else
                            {
                                foreach (var capture in captures)
                                {
                                    Console.WriteLine($"{fileName}: {new string(text.Data, capture.StartPosition, capture.EndPosition - capture.StartPosition)}");
                                }
                            }
                        }
                    }
                }
                catch (CompilationException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                catch (PegParsingException e)
                {
                    Console.WriteLine(e.Message);
                    return 2;
                }

                return 0;
            });

            return commandLineApplication.Execute(args);
        }

        private static List<string> CreateLibraryPaths(CommandOption libraryDirs)
        {
            var paths = new List<string>
                        {
                            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "peg"), // Built-in PEGs
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".peg") // User PEGs
                        };

            if (libraryDirs.HasValue())
            {
                foreach (var customPath in libraryDirs.Values)
                {
                    paths.Add(customPath.Trim());
                }
            }

            foreach (var customPath in Environment.GetEnvironmentVariable("PEG_LIBRARY_PATH")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? new string[0])
            {
                paths.Add(customPath.Trim());
            }

            paths.Add("."); // Current directory

            return paths;
        }

        private static IEnumerable<(string, Task<ContentCharData>)> LoadInputAsync(IEnumerable<string> args)
        {
            if (args.Count() <= 0)
            {
                yield return ("stdin", Task.Run(() => new StreamContentLoader(Console.In).ReadAllChars()));
            }
            else
            {
                foreach (var arg in args)
                {
                    yield return (arg, Task.Run(() => new UtfFileContentLoader(arg).ReadAllChars()));
                }
            }
        }

        private static IRunner BuildRunnerFromExpression(PatternCompiler patternCompiler, Operator expression)
        {
            return patternCompiler.Compile(new Pattern
            {
                Data = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, expression), new Any()))
            });
        }
    }
}