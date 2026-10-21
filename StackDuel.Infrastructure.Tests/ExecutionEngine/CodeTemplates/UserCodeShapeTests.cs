using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class UserCodeShapeTests
{
    private readonly JavaCodeTemplateStrategy _java = new();
    private readonly CppCodeTemplateStrategy _cpp = new();

    [Fact]
    public void Java_BareMethod_IsReportedBeforeCompiling()
    {
        string? problem = _java.DescribeUserCodeProblem(
            "public boolean isValid(String s) {\n    return true;\n}"
        );

        Assert.NotNull(problem);
        Assert.Contains("class Solution", problem);
    }

    [Fact]
    public void Java_SolutionClass_IsAccepted()
    {
        Assert.Null(
            _java.DescribeUserCodeProblem("class Solution {\n    public boolean isValid(String s) { return true; }\n}")
        );
    }

    [Fact]
    public void Java_PublicSolutionClass_IsAccepted()
    {
        Assert.Null(_java.DescribeUserCodeProblem("public class Solution { }"));
    }

    [Fact]
    public void Java_SolutionRecord_IsAccepted()
    {
        Assert.Null(_java.DescribeUserCodeProblem("record Solution() { }"));
    }

    [Fact]
    public void Java_DifferentlyNamedClass_IsReported()
    {
        Assert.NotNull(_java.DescribeUserCodeProblem("class Helper { }"));
    }

    [Fact]
    public void Cpp_BareFunction_IsReportedBeforeCompiling()
    {
        string? problem = _cpp.DescribeUserCodeProblem("bool isValid(string s) {\n    return true;\n}");

        Assert.NotNull(problem);
        Assert.Contains("class Solution", problem);
    }

    [Fact]
    public void Cpp_SolutionClass_IsAccepted()
    {
        Assert.Null(_cpp.DescribeUserCodeProblem("class Solution {\npublic:\n    bool isValid(string s);\n};"));
    }

    [Fact]
    public void Cpp_SolutionStruct_IsAccepted()
    {
        Assert.Null(_cpp.DescribeUserCodeProblem("struct Solution { bool isValid(string s); };"));
    }

    [Fact]
    public void DynamicLanguages_AcceptABareFunction()
    {
        ICodeTemplateStrategy python = new PythonCodeTemplateStrategy();
        ICodeTemplateStrategy javascript = new JavaScriptCodeTemplateStrategy();

        Assert.Null(python.DescribeUserCodeProblem("def is_valid(s):\n    return True"));
        Assert.Null(javascript.DescribeUserCodeProblem("function isValid(s) { return true; }"));
    }
}