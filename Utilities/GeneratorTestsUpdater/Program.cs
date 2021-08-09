using StrongInject.Generator.Tests.Unit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace GeneratorTestsUpdater
{
    /// <summary>
    /// This is a hacky utility that allows you to update all failing <see cref="GeneratorTests"/> when the way the Source Generator generates code changes.
    /// It uses heuristics and happens to work for the current set of tests, but may need updating as more tests are added.
    /// </summary>
    class Program
    {
        static void Main()
        {
            using var frontController = new XunitFrontController(AppDomainSupport.Denied, typeof(GeneratorTests).Assembly.Location);
            using var testDiscoveryVisitor = new TestDiscoverySink();
            frontController.Find(typeof(GeneratorTests).FullName, true, testDiscoveryVisitor, TestFrameworkOptions.ForDiscovery());
            testDiscoveryVisitor.Finished.WaitOne();

            using var testSourceUpdater = new TestSourceUpdater();
            using var manualResetEvent = new ManualResetEvent(false);
            frontController.RunTests(testDiscoveryVisitor.TestCases, testSourceUpdater, TestFrameworkOptions.ForExecution());
            testSourceUpdater.Execution.TestAssemblyFinishedEvent += _ => manualResetEvent.Set();
            manualResetEvent.WaitOne();
            testSourceUpdater.WriteFile();
        }

        public class TestSourceUpdater : TestMessageSink
        {
            private readonly string _targetFilePath;
            private string _source;
            private readonly string _originalSource;
            public TestSourceUpdater()
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                _targetFilePath = Directory.GetCurrentDirectory()[..(currentDirectory.LastIndexOf("bin"))] + "../../StrongInject.Tests.Unit/GeneratorTests.cs";
                _source = _originalSource = File.ReadAllText(_targetFilePath);
            }

            public override bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
            {
                var correctCodeRegex = new Regex(@", but\s*("".*"")\s* ((has a length of \d*)|(differs near ""))", RegexOptions.Singleline);
                var originalCodeRegex = new Regex(@"""#pragma.*?(?<!"")""(?!"")", RegexOptions.Singleline);
                var stackTraceRegex = new Regex(@"StrongInject.Generator.Tests.Unit.GeneratorTests.* (\d+)", RegexOptions.Singleline);
                if (message is ITestFailed { StackTraces: var stackTraces } testFailed)
                {
                    var match = correctCodeRegex.Match(testFailed.Messages.FirstOrDefault() ?? "");
                    if (match.Groups.Count >= 2)
                    {
                        var correctCode = match.Groups[1].Value;
                        correctCode = "\"" + correctCode[1..^1].Replace("\"", "\"\"") + "\"";
                        correctCode = Regex.Replace(correctCode, @"\r\n|\n\r|\n|\r", Environment.NewLine);

                        var stackTrace = stackTraces[0];
                        var line = int.Parse(stackTraceRegex.Match(stackTrace).Groups[1].Value);
                        var originalMethodLocation = _originalSource.IndexOf(testFailed.TestMethod.Method.Name);
                        var part = _originalSource[originalMethodLocation..(_originalSource.IndexOfNth('\n', line + 1))];
                        var count = originalCodeRegex.Matches(part).Count;
                        var methodLocation = _source.IndexOf(testFailed.TestMethod.Method.Name);
                        var sourceFromMethod = _source[methodLocation..];
                        var originalCodeMatches = originalCodeRegex.Matches(sourceFromMethod);
                        if (originalCodeMatches.Count == 0)
                        {
                            Console.WriteLine($"Cannot fix test {testFailed.TestMethod.Method.Name} as can't find string beginning '#pragma'");
                        }
                        else
                        {
                            var originalCodeMatch = originalCodeMatches[count];
                            _source = _source[..(methodLocation + originalCodeMatch.Index)] + correctCode + sourceFromMethod[(originalCodeMatch.Index + originalCodeMatch.Length)..];
                            Console.WriteLine($"Fixed test {testFailed.TestMethod.Method.Name}");
                        }
                    }
                }
                return base.OnMessageWithTypes(message, messageTypes);
            }

            public void WriteFile()
            {
                File.WriteAllText(_targetFilePath, _source);
            }
        }
    }

    public static class Extensions
    {
        public static int IndexOfNth(this string input, char value, int nth)
        {
            if (nth < 1)
                throw new NotSupportedException("Param 'nth' must be greater than 0!");

            var index = 0;

            for (int i = 1; i <= nth; i++)
            {
                index = input.IndexOf(value, startIndex: index + 1);
                if (index == -1)
                    return -1;
            }

            return index;
        }
    }
}
