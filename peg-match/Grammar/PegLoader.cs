using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PegMatch.Grammar
{
    public class PegLoader
    {
        public IReadOnlyList<string> BasePaths { get; }
        public CaptureKeyAllocator CaptureKeyAllocator { get; }

        private ExtendedPegGrammar grammar;

        private Dictionary<string, NamespaceContext> patterns = new Dictionary<string, NamespaceContext>();

        private int patternInstantiationCount = 0;
        
        public PegLoader(IEnumerable<string> paths, CaptureKeyAllocator captureKeyAllocator = null)
        {
            grammar = new ExtendedPegGrammar
            {
                CaptureKeyAllocator = captureKeyAllocator
            };

            BasePaths = paths.ToList();
            CaptureKeyAllocator = captureKeyAllocator;
        }

        public Operator Parse(string data)
        {
            var expression = grammar.ParseExpression(data);
            
            return ResolvePatternReferences(expression, null, new NamespaceContext());
        }

        public IRunner ParseAndCompile(string data)
        {
            return PatternCompiler.Default.Compile(new Pattern("root")
            {
                Data = Parse(data)
            });
        }

        private Operator ResolvePatternReferences(Operator op, IList<Pattern> localPatterns, NamespaceContext context)
        {
            var mapping = new Dictionary<Operator, Operator>();

            var stack = new Stack<Operator>();
            PushItem(stack, op);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is Pattern p && p.Data == null)
                {
                    var currentContext = context;
                    if (current is NamespacedPattern np)
                    {
                        currentContext = LoadNamespace(np.NamespacePath);
                    }

                    var localPatternReference = localPatterns?.FirstOrDefault(item => item.Name == p.Name);
                    if (localPatternReference != null)
                    {
                        mapping[current] = localPatterns.First(item => item.Name == p.Name);
                    }
                    else
                    {
                        var actualPattern = p is NamespacedPattern npp ? npp.FinalPattern : p;
                        var loadedPattern = currentContext.GetPattern(actualPattern, context == currentContext);

                        if (loadedPattern is ParameterizedPattern pp)
                        {
                            var currentPp = (ParameterizedPattern)actualPattern;
                            var innerMapping = new Dictionary<Operator, Operator>();

                            for (var i = 0; i < pp.Parameters.Count; i++)
                            {
                                var k = currentPp.Parameters[i];
                                if(mapping.TryGetValue(k, out var mappedValue))
                                {
                                    k = (Pattern)mappedValue;
                                }

                                innerMapping[pp.Parameters[i]] = k;
                            }

                            loadedPattern = RemapPattern(currentPp.Parameters, (ParameterizedPattern)loadedPattern, innerMapping);
                        }

                        mapping[current] = loadedPattern;
                    }
                }else if (current is InlinePattern ip)
                {
                    ip.Data = ResolvePatternReferences(ip.Data, localPatterns, context);
                }
                else
                {
                    foreach (var child in current.Children)
                    {
                        PushItem(stack, child);
                    }
                }
            }

            if (mapping.Count <= 0)
            {
                return op;
            }
            else
            {
                var duplicated = op.Duplicate(mapping);
                return duplicated;
            }
        }

        private static void PushItem(Stack<Operator> stack, Operator child)
        {
            stack.Push(child);
            if (child is NamespacedPattern np && np.FinalPattern is ParameterizedPattern npp)
            {
                foreach (var param in npp.Parameters)
                {
                    PushItem(stack, param);
                }
            }
            else if (child is ParameterizedPattern pp)
            {
                foreach (var param in pp.Parameters)
                {
                    PushItem(stack, param);
                }
            }
        }

        private Pattern RemapPattern(IList<Pattern> parameters, ParameterizedPattern loadedPattern, Dictionary<Operator, Operator> mapping)
        {
            foreach(var subPattern in loadedPattern.GetDescendants().OfType<ParameterizedPattern>())
            {
                mapping[subPattern] = RemapPattern(subPattern.Parameters, subPattern, mapping);
            }
            
            return new ParameterizedPattern($"{loadedPattern.Name}~{patternInstantiationCount++}", parameters.Select(item => (Pattern)item.Duplicate(mapping)))
            {
                Data = loadedPattern.Data.Duplicate(mapping.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            };
        }

        private NamespaceContext LoadNamespace(string ns)
        {
            if (!patterns.ContainsKey(ns))
            {
                foreach (var path in BasePaths)
                {
                    var file = Path.Combine(path, ns.Replace("::", $"{Path.DirectorySeparatorChar}") + ".peg");
                    if (File.Exists(file))
                    {
                        var result = grammar.ParseGrammar(File.ReadAllText(file)).ToList();
                        var context = patterns[ns] = new NamespaceContext();

                        foreach (var pattern in result)
                        {
                            context.Add(pattern);
                        }

                        foreach (var pattern in result)
                        {
                            pattern.Data = ResolvePatternReferences(pattern.Data, pattern is ParameterizedPattern pp ? pp.Parameters : null, context);
                        }

                        return context;
                    }
                }
            }

            return patterns[ns];
        }
    }
}
