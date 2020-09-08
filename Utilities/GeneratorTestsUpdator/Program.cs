using StrongInject.Generator.Tests.Unit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace GeneratorTestsUpdator
{
    /// <summary>
    /// This is a hacky utility that allows you to update all failing <see cref="GeneratorTests"/> when the way the Source Generator generates code changes.
    /// It uses heuristics and happens to work for the current set of tests, but may need updating as more tests are added.
    /// </summary>
    class Program
    {
        static void Main()
        {
            var frontController = new XunitFrontController(AppDomainSupport.Denied, typeof(GeneratorTests).Assembly.Location);
            var testDiscoveryVisitor = new TestDiscoverySink();
            frontController.Find(typeof(GeneratorTests).FullName, true, testDiscoveryVisitor, TestFrameworkOptions.ForDiscovery());
            testDiscoveryVisitor.Finished.WaitOne();

            var testSourceUpdater = new TestSourceUpdater();
            var manualResetEvent = new ManualResetEvent(false);
            frontController.RunTests(testDiscoveryVisitor.TestCases, testSourceUpdater, TestFrameworkOptions.ForExecution());
            testSourceUpdater.Execution.TestAssemblyFinishedEvent += _ => manualResetEvent.Set();
            manualResetEvent.WaitOne();
            testSourceUpdater.WriteFile();
        }

        public class TestSourceUpdater : TestMessageSink
        {
            private readonly string _targetFilePath;
            private string _source;
            public TestSourceUpdater()
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                _targetFilePath = Directory.GetCurrentDirectory()[..(currentDirectory.LastIndexOf("bin"))] + "../../StrongInject.Tests.Unit/GeneratorTests.cs";
                _source = File.ReadAllText(_targetFilePath);
            }

            public override bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
            {
                var correctCodeRegex = new Regex(@"with a length of \d*, but\s*("".*"")\s* has a length of \d*", RegexOptions.Singleline);
                var originalCodeRegex = new Regex(@"""#pragma.*?(?<!"")""(?!"")", RegexOptions.Singleline);
                if (message is ITestFailed testFailed)
                {
                    var match = correctCodeRegex.Match(testFailed.Messages.FirstOrDefault() ?? "");
                    if (match.Groups.Count == 2)
                    {
                        var correctCode = match.Groups[1].Value;
                        correctCode = "\"" + correctCode[1..^1].Replace("\"", "\"\"") + "\"";
                        correctCode = Regex.Replace(correctCode, @"\r\n|\n\r|\n|\r", Environment.NewLine);
                        var methodLocation = _source.IndexOf(testFailed.TestMethod.Method.Name);
                        var sourceFromMethod = _source[methodLocation..];
                        var originalCodeMatch = originalCodeRegex.Match(sourceFromMethod);
                        _source = _source[..(methodLocation + originalCodeMatch.Index)] + correctCode + sourceFromMethod[(originalCodeMatch.Index + originalCodeMatch.Length)..];
                        Console.WriteLine($"Fixed test {testFailed.TestMethod.Method.Name}");
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
}
