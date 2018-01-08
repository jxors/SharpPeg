using SharpPeg.Operators;
using RegexToPeg.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexToPeg
{
    public class RegexConverter
    {
        public Operator Convert(Expression expr)
        {
            return ConvertInternal(expr, new Empty());
        }

        private Operator ConvertInternal(Expression expr, Operator continuation)
        {
            switch (expr)
            {
                case null:
                    return continuation;
                case CharSet s:
                    var childA = new CharacterClass(s.Chars);
                    if (continuation is Empty)
                    {
                        return childA;
                    }

                    return new Sequence(childA, continuation);
                case AnyChar a:
                    // Not exactly correct, but correct enough for what it's needed for.
                    var excludeLineEndings = new Sequence(new Not(new CharacterClass(new CharacterRange((char)10, (char)13))), new Any());
                    if (continuation is Empty)
                    {
                        return excludeLineEndings;
                    }

                    return new Sequence(excludeLineEndings, continuation);
                case Concatenation c:
                    return ConvertInternal(c.ChildA, ConvertInternal(c.ChildB, continuation));
                case Union u:
                    return new PrioritizedChoice(ConvertInternal(u.ChildA, continuation), ConvertInternal(u.ChildB, continuation));
                case Star s:
                    if (s.Expression is CharSet cs && continuation is Empty _)
                    {
                        return new ZeroOrMore(ConvertInternal(s.Expression, continuation));
                    }
                    else
                    {
                        // Slower, memoizing
                        //var pattern = new Pattern();
                        //pattern.Data = new PrioritizedChoice(ConvertInternal(s.Expression, pattern), continuation);

                        //return pattern;

                        // Fast, iterative
                        return Operator.EndingWithGreedy(ConvertInternal(s.Expression, new Empty()), continuation);
                    }
                case Plus p:
                    return ConvertInternal(new Concatenation(p.Expression, new Star(p.Expression)), continuation);
                case Repeat r:
                    var inner = ConvertInternal(r.Expression, new Empty());
                    var range = r.RepeatRange;
                    var prefix = (Operator)new Empty();

                    if(range.Min >= 2)
                    {
                        prefix = new Sequence(Enumerable.Range(0, range.Min).Select(_ => inner));
                    }else if(range.Min == 1)
                    {
                        prefix = inner;
                    }

                    var count = range.Max - range.Min;
                    var postfix = new PrioritizedChoice(Enumerable.Range(0, count + 1)
                        .Select(n => count - n)
                        .Select(n => n == 0 ? continuation : new Sequence(
                            (n == 1 ? inner : new Sequence(
                                Enumerable.Range(0, n)
                                    .Select(_ => inner)
                            )), continuation))
                    );

                    return new Sequence(prefix, postfix);
                case Bos b:
                    // TODO!
                    return continuation;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
