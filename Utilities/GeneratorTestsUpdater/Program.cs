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
    /// This is a hacky utility that allows you to update all failing tests when the way the Source Generator generates code changes.
    /// It uses heuristics and happens to work for the current set of tests, but may need updating as more tests are added.
    /// </summary>
    class Program
    {
        static void Main()
        {
            using var frontController = new XunitFrontController(AppDomainSupport.Denied, typeof(GeneratorTests).Assembly.Location);
            using var testDiscoveryVisitor = new TestDiscoverySink();
            frontController.Find(true, testDiscoveryVisitor, TestFrameworkOptions.ForDiscovery());
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
            private readonly Dictionary<string, (string Original, string Updated)> _modifiedSourceByPath = new(StringComparer.OrdinalIgnoreCase);

            public override bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
            {
                var correctCodeRegex = new Regex(@", but\s*("".*"")\s* ((has a length of \d*)|(differs near ""))", RegexOptions.Singleline);
                var originalCodeRegex = new Regex(@"""#pragma.*?(?<!"")""(?!"")", RegexOptions.Singleline);
                if (message is ITestFailed { StackTraces: var stackTraces } testFailed)
                {
                    var match = correctCodeRegex.Match(testFailed.Messages.FirstOrDefault() ?? "");
                    if (match.Groups.Count >= 2)
                    {
                        var correctCode = match.Groups[1].Value;
                        correctCode = "\"" + correctCode[1..^1].Replace("\"", "\"\"") + "\"";
                        correctCode = Regex.Replace(correctCode, @"\r\n|\n\r|\n|\r", Environment.NewLine);

                        var stackTrace = stackTraces[0];
                        var escapedMethodName = Regex.Escape(testFailed.TestClass.Class.Name + '.' + testFailed.TestMethod.Method.Name);
                        var locationMatch = Regex.Match(stackTraces[0], escapedMethodName + @".* in (?<file>.*):line (?<line>\d+)", RegexOptions.Singleline);
                        if (!locationMatch.Success)
                            throw new NotImplementedException("Unable to parse file and line number from stack trace.");

                        var filePath = locationMatch.Groups["file"].Value;
                        var line = int.Parse(locationMatch.Groups["line"].Value);

                        lock (_modifiedSourceByPath)
                        {
                            if (!_modifiedSourceByPath.TryGetValue(filePath, out var contents))
                            {
                                contents.Updated = contents.Original = File.ReadAllText(filePath);
                            }

                            var originalMethodLocation = contents.Original.IndexOf(testFailed.TestMethod.Method.Name);
                            var part = contents.Original[originalMethodLocation..(contents.Original.IndexOfNth('\n', line + 1))];
                            var count = originalCodeRegex.Matches(part).Count;
                            var methodLocation = contents.Updated.IndexOf(testFailed.TestMethod.Method.Name);
                            var sourceFromMethod = contents.Updated[methodLocation..];
                            var originalCodeMatches = originalCodeRegex.Matches(sourceFromMethod);
                            if (originalCodeMatches.Count == 0)
                            {
                                Console.WriteLine($"Cannot fix test {testFailed.TestMethod.Method.Name} as can't find string beginning '#pragma'");
                            }
                            else
                            {
                                var originalCodeMatch = originalCodeMatches[count];
                                contents.Updated = contents.Updated[..(methodLocation + originalCodeMatch.Index)] + correctCode + sourceFromMethod[(originalCodeMatch.Index + originalCodeMatch.Length)..];
                                _modifiedSourceByPath[filePath] = contents;

                                Console.WriteLine($"Fixed test {testFailed.TestMethod.Method.Name}");
                            }
                        }
                    }
                }
                return base.OnMessageWithTypes(message, messageTypes);
            }

            public void WriteFile()
            {
                lock (_modifiedSourceByPath)
                {
                    foreach (var (path, (_, updatedContents)) in  _modifiedSourceByPath)
                        File.WriteAllText(path, updatedContents);

                    _modifiedSourceByPath.Clear();
                }
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
