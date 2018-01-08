using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegMatch.Grammar;
using SharpPeg.Runner;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    public abstract class PegTest
    {
        protected PegLoader Loader { get; } = new PegLoader(new[] { "./peg/" });

        protected void MustMatch(string pattern, string text)
        {
            var runner = Loader.ParseAndCompile(pattern);
            var captures = new List<Capture>();
            var result = runner.Run(text, captures);

            Assert.IsTrue(result.IsSuccessful, $"{pattern} must match {text} (match failed)");
            Assert.IsTrue(result.InputPosition >= text.Length, $"{pattern} must match {text} (matched only {result.InputPosition} out of {text.Length} characters)");
        }

        protected void MustNotMatch(string pattern, string text)
        {
            var runner = Loader.ParseAndCompile(pattern);
            var result = runner.Run(text);

            Assert.IsFalse(result.IsSuccessful && result.InputPosition >= text.Length, $"{pattern} must not match {text}");
        }
    }
}
