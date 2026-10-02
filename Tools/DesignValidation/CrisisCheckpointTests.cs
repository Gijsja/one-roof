using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System.Reflection;
var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
runner.Load(Assembly.GetExecutingAssembly(), new Dictionary<string, object>());
var result = runner.Run(TestListener.NULL, TestFilter.Empty);
File.WriteAllText(args.Length > 0 ? args[0] : "/tmp/c0-standalone-tests.xml", result.ToXml(true).OuterXml);
Console.WriteLine($"Total={result.Test.TestCaseCount} Passed={result.PassCount} Failed={result.FailCount} Skipped={result.SkipCount}");

return result.FailCount == 0 && result.Test.TestCaseCount > 0 ? 0 : 1;
