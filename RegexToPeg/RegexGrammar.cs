using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;
using SharpPeg.SelfParser;
using RegexToPeg.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegexToPeg
{
    public class RegexGrammar
    {
        public IRunner Parser { get; }

        public RegexGrammar(PatternCompiler patternCompiler)
        {
            var RE = new Pattern("RE");
            var simpleRE = new Pattern("SimpleRE");

            var metaCharacter = new Pattern("metaCharacter")
            {
                Data = new PrioritizedChoice(
                    new CharacterClass('*', '+', '^', '$', '|', '(', ')', '[', ']'),
                    new Sequence(new CharacterClass('\\'), new CharacterClass('t', 'n', 'r', 'f', 'b', 'B', 'd', 'D', 's', 'S', 'w', 'W', 'Q', 'U', 'L')),
                    CharacterClass.String(@"*?"),
                    CharacterClass.String(@"+?"),
                    CharacterClass.String(@"$`"),
                    CharacterClass.String(@"$'"),
                    CharacterClass.String(@"$&"),
                    CharacterClass.String(@"\cX"),
                    new Sequence(new CharacterClass('\\', '$'), CharacterClass.Range('0', '9')),
                    new Sequence(new CharacterClass('\\'), CharacterClass.Range('0', '7'), CharacterClass.Range('0', '7'), CharacterClass.Range('0', '7'))
                )
            };

            var allowedMetaCharacters = new Pattern("allowedMetaCharacter")
            {
                Data = new CaptureGroup((int)CaptureType.MetaCharacter,
                    new PrioritizedChoice(
                        new Sequence(new CharacterClass('\\'), new CharacterClass('t', 'n', 'r', 'f', 'b', 'B', 'd', 'D', 's', 'S', 'w', 'W', 'Q', 'U', 'L')),
                        CharacterClass.String(@"*?"),
                        CharacterClass.String(@"+?"),
                        CharacterClass.String(@"$`"),
                        CharacterClass.String(@"$'"),
                        CharacterClass.String(@"$&"),
                        CharacterClass.String(@"\cX"),
                        new Sequence(new CharacterClass('\\', '$'), CharacterClass.Range('0', '9')),
                        new Sequence(new CharacterClass('\\'), CharacterClass.Range('0', '7'), CharacterClass.Range('0', '7'), CharacterClass.Range('0', '7'))
                    )
                )
            };

            var character = new Pattern("character")
            {
                Data = new CaptureGroup((int)CaptureType.Char,
                    new PrioritizedChoice(
                        new Sequence(
                            new CharacterClass('\\'),
                            metaCharacter
                        ),
                        new Sequence(
                            new Not(metaCharacter),
                            new Any()
                        )
                    )
                )
            };

            var range = new CaptureGroup((int)CaptureType.Range, new Sequence(character, new CharacterClass('-'), character));
            var setItem = new PrioritizedChoice(range, character);
            var setItems = new Pattern() { Data = Operator.OneOrMore(setItem) };
            var positiveSet = new CaptureGroup((int)CaptureType.PositiveSet, new Sequence(new CharacterClass('['), setItems, new CharacterClass(']')));
            var negativeSet = new CaptureGroup((int)CaptureType.NegativeSet, new Sequence(CharacterClass.String("[^"), setItems, new CharacterClass(']')));
            var set = new Pattern("set") { Data = new PrioritizedChoice(negativeSet, positiveSet) };
            var eos = new CaptureGroup((int)CaptureType.Eos, new CharacterClass('$'));
            var any = new CaptureGroup((int)CaptureType.Any, new CharacterClass('.'));
            var group = new Sequence(new CharacterClass('('), RE, new CharacterClass(')'));

            var elementaryRE = new Pattern("elementaryRE")
            {
                Data = new PrioritizedChoice(group, any, eos, set, character, allowedMetaCharacters)
            };

            var number = Operator.OneOrMore(CharacterClass.Range('0', '9'));
            var repeatRange =  new Sequence(new CharacterClass('{'), new CaptureGroup((int)CaptureType.RepeatRange, new Sequence(number, Operator.Optional(new Sequence(new CharacterClass(','), number)))), new CharacterClass('}'));

            var plus = new Pattern("plus") { Data = new CaptureGroup((int)CaptureType.Plus, new Sequence(elementaryRE, new CharacterClass('+'))) };
            var star = new Pattern("star") { Data = new CaptureGroup((int)CaptureType.Star, new Sequence(elementaryRE, new CharacterClass('*'))) };
            var repeat = new Pattern("repeat") { Data = new CaptureGroup((int)CaptureType.Repeat, new Sequence(elementaryRE, repeatRange)) };
            var basicRE = new PrioritizedChoice(star, plus, repeat, elementaryRE);
            simpleRE.Data = new CaptureGroup((int)CaptureType.Concatenation, Operator.OneOrMore(basicRE));
            
            RE.Data = new CaptureGroup((int)CaptureType.Union, new Sequence(simpleRE, new ZeroOrMore(new Sequence(new CharacterClass('|'), RE))));

            Parser = patternCompiler.Compile(RE);
        }

        public Expression ParseExpression(string data)
        {
            return Parse<Expression>(Parser, data);
        }

        protected T Parse<T>(IRunner runner, string data) where T : class
        {
            var captures = new List<Capture>();
            var result = runner.Run(data, captures);

            if (result.IsSuccessful)
            {
                var iterator = new CaptureIterator<Expression>(data, captures);
                var output = BuildTree(iterator);
                if (output as T == null)
                {
                    throw new PegParsingException($"Unable to parse PEG: {output}");
                }

                return output as T;
            }
            else
            {
                var near = data.Substring(result.InputPosition);
                if (near.Length > 10)
                {
                    near = near.Substring(0, 10);
                }

                throw new PegParsingException($"Parsing error at character {result.InputPosition}. {runner.ExplainResult(result, data)} near {near}");
            }
        }

        private Expression BuildTree(CaptureIterator<Expression> iterator)
        {
            var result = iterator.Iterate(BuildTreeNode).ToList();
            if(result.Count != 1)
            {
                throw new PegParsingException($"Parsed {result.Count} regex expressions, expected 1 expression.");
            }

            return result.First();
        }

        protected virtual Expression BuildTreeNode(int intKey, string captureData, IReadOnlyList<Expression> parameters)
        {
            var key = (CaptureType)intKey;

            switch (key)
            {
                case CaptureType.Union:
                    if(parameters.Count <= 1)
                    {
                        return parameters[0];
                    }

                    return new Union(parameters);
                case CaptureType.Concatenation:
                    if (parameters.Count <= 1)
                    {
                        return parameters[0];
                    }

                    return new Concatenation(parameters);
                case CaptureType.NegativeSet:
                    var not = new HashSet<char>(parameters.SelectMany(item => GetAllChars(item)));
                    return new CharSet(Enumerable.Range(char.MinValue, char.MaxValue).Select(c => (char)c).Where(c => !not.Contains(c)));
                case CaptureType.PositiveSet:
                    return new CharSet(parameters.SelectMany(item => GetAllChars(item)));
                case CaptureType.Char:
                    if (captureData.Length == 1)
                    {
                        return new CharSet(captureData[0]);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                case CaptureType.Eos:
                    return new Eos();
                case CaptureType.Any:
                    return new AnyChar();
                case CaptureType.Group:
                    if(parameters.Count == 1)
                    {
                        return parameters[0];
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                case CaptureType.Plus:
                    return new Plus(parameters[0]);
                case CaptureType.Star:
                    return new Star(parameters[0]);
                case CaptureType.MetaCharacter:
                    var wordChars = Enumerable.Range(char.MinValue, char.MaxValue).Select(c => (char)c).Where(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')).ToArray();
                    switch (captureData)
                    {
                        case @"\w":
                            return new CharSet(wordChars);
                        case "^":
                            return new Bos();
                        case @"\b":
                            var notWordChars = Enumerable.Range(char.MinValue, char.MaxValue).Select(c => (char)c).Where(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))).ToArray();
                            return new CharSet(notWordChars);
                        case @"\s":
                            return new CharSet(' ', '\t', '\r', '\n', '\f');
                        default:
                            throw new NotImplementedException();
                    }
                case CaptureType.Range:
                    return new Range(((CharSet)parameters[0]).Chars.First(), ((CharSet)parameters[1]).Chars.First());
                case CaptureType.Repeat:
                    return new Repeat(parameters[0], (RepeatRange)parameters[1]);
                case CaptureType.RepeatRange:
                    if (captureData.Contains(','))
                    {
                        var nums = captureData.Split(',').Select(item => int.Parse(item.Trim())).ToArray();
                        return new RepeatRange(nums[0], nums[1]);
                    }
                    else
                    {
                        var num = int.Parse(captureData);
                        return new RepeatRange(num, num);
                    }
                default:
                    throw new ArgumentException(nameof(intKey));
            }
        }

        private IEnumerable<char> GetAllChars(Expression item)
        {
            if(item is CharSet s)
            {
                return s.Chars;
            }else if(item is Range r)
            {
                return Enumerable.Range(r.Min, r.Max - r.Min + 1).Select(c => (char)c);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
