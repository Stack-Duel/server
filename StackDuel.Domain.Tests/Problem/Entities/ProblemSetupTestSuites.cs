using System.Reflection;
using StackDuel.Domain.TestSuites.Enums;
using ProblemSetupEntity = StackDuel.Domain.Problems.Entities.ProblemSetup;
using TestSuiteEntity = StackDuel.Domain.TestSuites.Entities.TestSuite;

namespace StackDuel.Domain.Tests.Problem.Entities;

internal static class ProblemSetupTestSuites
{
    private static readonly FieldInfo TestSuitesField = typeof(ProblemSetupEntity).GetField(
        "_testSuites",
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    public static TestSuiteEntity Attach(ProblemSetupEntity setup, TestSuiteType type)
    {
        TestSuiteEntity testSuite = new($"{type} suite", type);
        ((List<TestSuiteEntity>)TestSuitesField.GetValue(setup)!).Add(testSuite);
        return testSuite;
    }
}
