using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Text;
using SharpPeg.Runner;
using SharpPeg.Operators;
using System.Linq;
using SharpPeg;

namespace PegMatch.Grammar
{
    public class ExtendedPegGrammar : PegGrammar
    {
        private Pattern captureGroup;

        public CaptureKeyAllocator CaptureKeyAllocator { get; internal set; }

        enum ExtendedCaptureKeys
        {
            Namespace = 100,
            ParameterizedIdentifier = 101,
            InlinePattern = 102,
            PatternParameterReference = 103,
            FixedRepeat = 104,
            Export = 105,
            CaptureGroup = 106,
            CaptureGroupName = 107,
        }
        public ExtendedPegGrammar()
        { }

        public ExtendedPegGrammar(PatternCompiler patternCompiler) : base(patternCompiler)
        { }

        protected override void Build()
        {
            base.Build();
            var simpleIdentifier = new Pattern("simpleIdentifier") { Data = identifier.Data };

            var identifierOrInline = new Pattern("identifierOrInline")
            {
                Data = new PrioritizedChoice(identifier,
                    new CaptureGroup((int)ExtendedCaptureKeys.InlinePattern,
                        new Sequence(
                            "\\(",
                            spacing,
                            internalExpression,
                            ')'
                        )
                    )
                )
            };

            // Prevent 'export' from being seen as a non-terminal
            // This effectively reserves export as a global keyword
            prefix.Data = new Sequence(new Not(CharacterClass.String("export")), prefix.Data);

            var parameterizedIdentifier = BuildParameterizedIdentifier(simpleIdentifier, identifierOrInline);
            var parameterizedIdentifierDeclaration = BuildParameterizedIdentifier(simpleIdentifier, new CaptureGroup((int)ExtendedCaptureKeys.PatternParameterReference, simpleIdentifier));

            var complexIdentifier = new Pattern("complexIdentifier") { Data = new PrioritizedChoice(parameterizedIdentifier, simpleIdentifier) };

            var namespacedIdentifier = new Pattern("namespacedIdentifier");
            namespacedIdentifier.Data = new PrioritizedChoice(
                new CaptureGroup((int)ExtendedCaptureKeys.Namespace, 
                    new Sequence(simpleIdentifier, "::", namespacedIdentifier)
                ), 
                complexIdentifier
            );

            identifier.Data = namespacedIdentifier;

            var possibleDeclarationIdentifiers = new PrioritizedChoice(parameterizedIdentifierDeclaration, simpleIdentifier);
            identifierDeclaration.Data = new PrioritizedChoice(
                new CaptureGroup((int)ExtendedCaptureKeys.Export, 
                    new Sequence("export", spacing, possibleDeclarationIdentifiers)
                ), 
                possibleDeclarationIdentifiers
            );

            var number = Operator.OneOrMore(new CharacterClass(new CharacterRange('0', '9')));
            var fixedRepeat = new CaptureGroup((int)ExtendedCaptureKeys.FixedRepeat, new Sequence(primary, '^', number, Operator.Optional(new Sequence("..", number))));
            suffix.Data = new PrioritizedChoice(new Sequence(fixedRepeat, spacing), suffix.Data);

            
        }

        private void BuildCaptureGroup()
        {
            var captureGroupIdentifier = new Pattern("captureGroupIdentifier")
            {
                Data = new Sequence(
                    new CaptureGroup((int)ExtendedCaptureKeys.CaptureGroupName,
                        new Sequence(
                            Operator.OneOrMore(
                                new CharacterClass(new[] { ('A', 'Z'), ('a', 'z'), ('0', '9') }, '_', '-')
                            ),
                            Operator.Optional("[]")
                        )
                    ),
                    spacing
                )
            };

            captureGroup = new Pattern("captureGroup")
            {
                Data = new CaptureGroup((int)ExtendedCaptureKeys.CaptureGroup,
                    new Sequence(
                        '{',
                        spacing,
                        captureGroupIdentifier,
                        ':',
                        spacing,
                        internalExpression,
                        '}',
                        spacing
                    )
                )
            };
        }

        protected override IEnumerable<Operator> GetPrimaryData()
        {
            BuildCaptureGroup();
            yield return captureGroup;

            foreach (var item in base.GetPrimaryData())
            {
                yield return item;
            }
        }

        private Pattern BuildParameterizedIdentifier(Pattern simpleIdentifier, Operator identifierType)
        {
            return new Pattern("parameterizedIdentifier")
            {
                Data = new CaptureGroup((int)ExtendedCaptureKeys.ParameterizedIdentifier,
                    new Sequence(
                        simpleIdentifier,
                        spacing,
                        '<',
                        spacing,
                        identifierType,
                        new ZeroOrMore(new Sequence(',', spacing, identifierType)),
                        '>',
                        spacing
                    )
                )
            };
        }

        protected override Operator BuildTreeNode(int intKey, string captureData, IReadOnlyList<Operator> parameters)
        {
            if (intKey == (int)ExtendedCaptureKeys.Namespace)
            {
                var beginning = parameters[0] as Pattern;
                var ending = parameters[1] as Pattern;

                return new NamespacedPattern(beginning.Name, ending);
            }
            else if (intKey == (int)ExtendedCaptureKeys.ParameterizedIdentifier)
            {
                var basePattern = parameters[0] as Pattern;
                return new ParameterizedPattern(basePattern.Name, parameters.Skip(1).Cast<Pattern>());
            }
            else if (intKey == (int)ExtendedCaptureKeys.InlinePattern)
            {
                return new InlinePattern { Data = parameters[0] };
            }
            else if (intKey == (int)ExtendedCaptureKeys.PatternParameterReference)
            {
                return new PatternParameterReference(((Pattern)parameters[0]).Name);
            }
            else if (intKey == (int)ExtendedCaptureKeys.FixedRepeat)
            {
                var str = new string(captureData.Reverse().TakeWhile(c => c != '^').Reverse().ToArray());
                if (str.Contains(".."))
                {
                    var parts = str.Split(new[] { ".." }, StringSplitOptions.None).Select(item => int.Parse(item)).ToArray();
                    var repeatMin = parts[0];
                    var repeatMax = parts[1];

                    if(repeatMax < repeatMin)
                    {
                        throw new ArgumentOutOfRangeException("repeatMax must be greater than or equal to repeatMin");
                    }

                    if (repeatMin == repeatMax)
                    {
                        return Operator.Repeat(repeatMin, parameters[0]);
                    }
                    else
                    {
                        return new PrioritizedChoice(
                            Enumerable.Range(repeatMin, repeatMax - repeatMin + 1).Select(n => Operator.Repeat(repeatMax - n + 1, parameters[0]))
                        );
                    }
                }
                else
                {
                    var repeatAmount = int.Parse(str);
                    return Operator.Repeat(repeatAmount, parameters[0]);
                }
            }
            else if (intKey == (int)ExtendedCaptureKeys.Export)
            {
                if (parameters[0] is ExportablePattern extendedPattern)
                {
                    extendedPattern.IsPublic = true;
                    return extendedPattern;
                }
                else
                {
                    var pattern = (Pattern)parameters[0];
                    return new ExportablePattern(pattern.Name, true)
                    {
                        Data = pattern.Data
                    };
                }
            }
            else if (intKey == (int)ExtendedCaptureKeys.CaptureGroupName)
            {
                return new NamedCaptureGroup(captureData, CaptureKeyAllocator?[captureData] ?? 0, null);
            }else if (intKey == (int)ExtendedCaptureKeys.CaptureGroup)
            {
                if(CaptureKeyAllocator == null)
                {
                    return parameters[1];
                }

                var namedGroup = (NamedCaptureGroup)parameters[0];
                return new NamedCaptureGroup(namedGroup.Name, namedGroup.Key, parameters[1]);
            }

            return base.BuildTreeNode(intKey, captureData, parameters);
        }
    }
}
