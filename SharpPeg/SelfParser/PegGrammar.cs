using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpPeg.Runner;

namespace SharpPeg.SelfParser
{
    public class PegGrammar
    {
        private readonly PatternCompiler patternCompiler;
        private IRunnerFactory grammarRunner;
        private IRunnerFactory expressionRunner;

        protected Pattern OPEN;
        protected Pattern CLOSE;
        protected Pattern PLUS;
        protected Pattern STAR;
        protected Pattern QUESTION;
        protected Pattern NOT;
        protected Pattern AND;
        protected Pattern DOT;
        private Pattern empty;
        protected Pattern SLASH;
        protected Pattern endOfFile;
        protected Pattern endOfLine;
        protected Pattern space;
        protected Pattern comment;
        protected Pattern spacing;
        protected Pattern LEFTARROW;
        protected Pattern character;
        protected Pattern characterClass;
        protected Pattern range;
        protected Pattern literal;
        protected Pattern primary;
        protected Pattern identifier;
        protected Pattern identifierDeclaration;
        protected Pattern suffix;
        protected Pattern prefix;
        protected Pattern sequence;
        protected Pattern internalExpression;

        public Pattern Grammar { get; private set; }
        protected Pattern Definition;
        public Pattern Expression { get; private set; }
        private bool m_IsBuilt;

        public PegGrammar(PatternCompiler compilerFactory)
        {
            m_IsBuilt = false;
            patternCompiler = compilerFactory;
        }

        public PegGrammar() : this(PatternCompiler.Default)
        { }

        protected virtual void Build()
        {
            endOfFile = new Pattern(nameof(endOfFile))
            {
                Data = new Not(new Any())
            };

            endOfLine = new Pattern(nameof(endOfFile))
            {
                Data = new PrioritizedChoice("\r\n", '\n', '\r')
            };

            space = new Pattern(nameof(space))
            {
                Data = new PrioritizedChoice(new CharacterClass(' ', '\t'), endOfLine)
            };

            comment = new Pattern(nameof(comment))
            {
                Data = new Sequence(
                    '#',
                    new ZeroOrMore(new Sequence(new Not(endOfLine), new Any())),
                    endOfLine
                )
            };

            spacing = new Pattern(nameof(spacing))
            {
                Data = new ZeroOrMore(new PrioritizedChoice(space, comment))
            };

            DOT = new Pattern(".")
            {
                Data = new CaptureGroup((int)CaptureType.Any, new Sequence('.', spacing))
            };

            empty = new Pattern(nameof(empty))
            {
                Data = new CaptureGroup((int)CaptureType.Empty, new Sequence('e', spacing))
            };

            OPEN = SingleCharacterWithSpacing('(');
            CLOSE = SingleCharacterWithSpacing(')');
            PLUS = SingleCharacterWithSpacing('+');
            STAR = SingleCharacterWithSpacing('*');
            QUESTION = SingleCharacterWithSpacing('?');
            NOT = SingleCharacterWithSpacing('!');
            AND = SingleCharacterWithSpacing('&');
            SLASH = SingleCharacterWithSpacing('/');

            LEFTARROW = new Pattern("<-")
            {
                Data = new Sequence("<-", spacing)
            };

            // TODO: Properly parse special chars
            character = new Pattern(nameof(character))
            {
                Data = new CaptureGroup((int)CaptureType.Character, new PrioritizedChoice(
                        new Sequence('\\', new CharacterClass('n', 'r', 't', '\'', '"', '[', ']', '\\', '-')),
                        new Sequence('\\', CharacterClass.Range('0', '2'), CharacterClass.Range('0', '7'), CharacterClass.Range('0', '7')),
                        new Sequence('\\', CharacterClass.Range('0', '7'), Operator.Optional(CharacterClass.Range('0', '7'))),
                        new Sequence(new Not(new CharacterClass('\\')), new Any())
                    )
                )
            };

            range = new Pattern(nameof(range))
            {
                Data = new CaptureGroup((int)CaptureType.CharacterClassRange, new PrioritizedChoice(new Sequence(character, new CharacterClass('-'), character), character))
            };

            characterClass = new Pattern(nameof(characterClass))
            {
                Data = new CaptureGroup((int)CaptureType.CharacterClass,
                    new Sequence(
                        '[',
                        new ZeroOrMore(new Sequence(new Not(new CharacterClass(']')), range)),
                        ']',
                        spacing
                    )
                )
            };

            literal = new Pattern(nameof(literal))
            {
                Data = new Sequence(
                    new CaptureGroup((int)CaptureType.Literal,
                        new PrioritizedChoice(
                            new Sequence(
                                '\'',
                                new ZeroOrMore(new Sequence(new Not('\''), character)),
                                '\''
                            ),
                            new Sequence(
                                '"',
                                new ZeroOrMore(new Sequence(new Not('"'), character)),
                                '"'
                            )
                        )
                    ),
                    spacing
                )
            };

            primary = new Pattern();
            identifier = new Pattern(nameof(identifier))
            {
                Data = new Sequence(new CaptureGroup((int)CaptureType.Identifier, 
                    new Sequence(
                        new CharacterClass(new CharacterRange('A', 'Z'), new CharacterRange('a', 'z'), new CharacterRange('_', '_')), 
                        new ZeroOrMore(new CharacterClass(new CharacterRange('A', 'Z'), new CharacterRange('a', 'z'), new CharacterRange('0', '9'), new CharacterRange('_', '_')))
                    )
                ), spacing)
            };

            identifierDeclaration = new Pattern(nameof(identifierDeclaration))
            {
                Data = identifier
            };

            suffix = new Pattern(nameof(suffix))
            {
                Data = new PrioritizedChoice(
                    new CaptureGroup((int)CaptureType.Optional, new Sequence(primary, QUESTION)),
                    new CaptureGroup((int)CaptureType.ZeroOrMore, new Sequence(primary, STAR)),
                    new CaptureGroup((int)CaptureType.OneOrMore, new Sequence(primary, PLUS)),
                    primary
                )
            };

            prefix = new Pattern(nameof(prefix))
            {
                Data = new PrioritizedChoice(
                    new CaptureGroup((int)CaptureType.And, new Sequence(AND, suffix)),
                    new CaptureGroup((int)CaptureType.Not, new Sequence(NOT, suffix)),
                    suffix
                )
            };

            sequence = new Pattern(nameof(sequence))
            {
                Data = new CaptureGroup((int)CaptureType.Sequence, new ZeroOrMore(prefix))
            };

            internalExpression = new Pattern(nameof(internalExpression))
            {
                Data = new CaptureGroup((int)CaptureType.PrioritizedChoice, new Sequence(sequence, new ZeroOrMore(new Sequence(SLASH, sequence))))
            };

            primary.Data = new PrioritizedChoice(GetPrimaryData());

            this.Definition = new Pattern(nameof(Definition))
            {
                Data = new CaptureGroup((int)CaptureType.Definition, new Sequence(identifierDeclaration, LEFTARROW, internalExpression))
            };

            this.Grammar = new Pattern(nameof(Grammar))
            {
                Data = new Sequence(spacing, Operator.OneOrMore(Definition), endOfFile)
            };

            this.Expression = new Pattern(nameof(Expression))
            {
                Data = new Sequence(internalExpression.Data, endOfFile)
            };
        }

        protected Pattern SingleCharacterWithSpacing(char c)
        {
            return new Pattern($"{c}")
            {
                Data = new Sequence(c, spacing)
            };
        }

        protected virtual IEnumerable<Operator> GetPrimaryData()
        {
            yield return empty;
            yield return new Sequence(identifier, new Not(LEFTARROW));
            yield return new Sequence(OPEN, internalExpression, CLOSE);
            yield return literal;
            yield return characterClass;
            yield return DOT;
        }

        private void EnsureData()
        {
            if(!m_IsBuilt)
            {
                Build();
                m_IsBuilt = true;
            }
        }

        private void EnsureBuilt(Pattern pattern, ref IRunnerFactory runner)
        {
            if (runner == null)
            {
                runner = patternCompiler.CompileAsFactory(pattern);
            }
        }

        public void EnsureExpressionBuilt()
        {
            EnsureData();
            EnsureBuilt(Expression, ref expressionRunner);
        }

        public void EnsureGrammarBuilt()
        {
            EnsureData();
            EnsureBuilt(Grammar, ref grammarRunner);
        }

        public IEnumerable<Pattern> ParseGrammar(string data)
        {
            EnsureGrammarBuilt();

            return Parse(grammarRunner.New(), data).Cast<Pattern>();
        }

        public Operator ParseExpression(string data)
        {
            EnsureExpressionBuilt();

            var result = Parse(expressionRunner.New(), data);
            if(result.Count != 1)
            {
                throw new PegParsingException("Parsed more than one expression");
            }

            return result.First();
        }

        protected List<Operator> Parse(IRunner runner, string data)
        {
            var captures = new List<Capture>();
            var result = runner.Run(data, captures);

            if (!result.IsSuccessful || result.InputPosition < data.Length)
            {
                throw new PegParsingException($"Parsing error at character {result.InputPosition}. {runner.ExplainResult(result, data)}");
            }

            var iterator = new CaptureIterator<Operator>(data, captures);
            var output = BuildTree(iterator).ToList();

            if(output.Count <= 0)
            {
                throw new PegParsingException($"Unable to parse PEG: {output}");
            }

            return output;
        }

        private IEnumerable<Operator> BuildTree(CaptureIterator<Operator> iterator)
        {
            return iterator.Iterate(BuildTreeNode);
        }

        protected virtual Operator BuildTreeNode(int intKey, string captureData, IReadOnlyList<Operator> parameters)
        {
            var key = (CaptureType)intKey;

            switch (key)
            {
                case CaptureType.And: return Operator.And(parameters[0]);
                case CaptureType.Any: return new Any();
                case CaptureType.CharacterClass:
                    var rangeParams = parameters.OfType<CharacterClass>();
                    return new CharacterClass(rangeParams.SelectMany(p => p.Ranges));
                case CaptureType.CharacterClassRange:
                    if (parameters.Count == 1)
                    {
                        return parameters[0];
                    }
                    else
                    {
                        var min = (CharacterClass)parameters[0];
                        var max = (CharacterClass)parameters[1];
                        
                        if(min.NumChars > 1 || max.NumChars > 1)
                        {
                            throw new PegParsingException($"Cannot create range from {min} and {max}");
                        }

                        return CharacterClass.Range(min.Value.First(), max.Value.Last());
                    }
                case CaptureType.Definition:
                    var identifier = parameters[0] as Pattern;
                    identifier.Data = parameters[1];

                    return identifier;
                case CaptureType.Empty: return new Empty();
                case CaptureType.Identifier:
                    return new UnresolvedPatternReference(captureData);
                case CaptureType.Character:
                    if(captureData.Length == 1)
                    {
                        return new CharacterClass(captureData[0]);
                    }
                    else
                    {
                        return new CharacterClass(TranslateEscapeCharacter(captureData));
                    }
                case CaptureType.Literal:
                    if(parameters.Count <= 0)
                    {
                        return new Empty();
                    }else if(parameters.Count == 1)
                    {
                        return parameters[0];
                    }
                    else
                    {
                        return new Sequence(parameters);
                    }
                case CaptureType.Not: return new Not(parameters[0]);
                case CaptureType.OneOrMore: return Operator.OneOrMore(parameters[0]);
                case CaptureType.Optional: return Operator.Optional(parameters[0]);
                case CaptureType.ZeroOrMore: return new ZeroOrMore(parameters[0]);
                case CaptureType.PrioritizedChoice:
                    if (parameters.Count <= 1)
                    {
                        return parameters[0];
                    }

                    if(parameters.All(item => item is CharacterClass))
                    {
                        return new CharacterClass(parameters.Cast<CharacterClass>().SelectMany(item => item.Ranges));
                    }

                    return new PrioritizedChoice(parameters);
                case CaptureType.Sequence:
                    if (parameters.Count <= 1)
                    {
                        return parameters[0];
                    }

                    return new Sequence(parameters);
                default:
                    throw new ArgumentOutOfRangeException($"Unrecognised CaptureType {key}");
            }
        }

        private static char TranslateEscapeCharacter(string captureData)
        {
            switch (captureData)
            {
                case @"\n": return '\n';
                case @"\r": return '\r';
                case @"\t": return '\t';
                case @"\'": return '\'';
                case "\\\"": return '"';
                case @"\[": return '[';
                case @"\]": return ']';
                case @"\-": return '-';
                case @"\\": return '\\';
                default:
                    var result = Convert.ToInt32(captureData.Substring(1), 8);
                    return (char)result;
            }
        }
    }
}
