using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates.ValueTypes;
using System.Text.RegularExpressions;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed partial class CppCodeTemplateStrategy : ICodeTemplateStrategy
{
    private static readonly Dictionary<string, (string CppType, string ParseFn)> TypesByValueType = new()
    {
        ["integer"] = ("int", "parseInt"),
        ["double"] = ("double", "parseDouble"),
        ["boolean"] = ("bool", "parseBool"),
        ["string"] = ("string", "parseString"),
        ["integer_array"] = ("vector<int>", "parseIntArray"),
    };

    public string LanguageName => "C++";

    public IReadOnlySet<ValueTypeKind> SupportedKinds => CppValueTypeRenderer.SupportedKinds;

    public string BuildStdin(IReadOnlyList<CodeTemplateInput> inputs) => string.Join("\n", inputs.Select(i => i.Value));

    private static (string CppType, string ParseFn, ValueTypeSpec? Spec) ResolveCppType(CodeTemplateInput input)
    {
        if (input.Spec is not null)
            return (
                CppValueTypeRenderer.RenderType(input.Spec),
                CppValueTypeRenderer.ParserName(input.Spec),
                input.Spec
            );

        if (!TypesByValueType.TryGetValue(input.ValueType, out (string CppType, string ParseFn) mapped))
            throw new NotSupportedException($"Unsupported C++ value type '{input.ValueType}'.");

        ValueTypeSpec? legacySpec = WellKnownValueTypes.All.FirstOrDefault(t => t.Key == input.ValueType)?.Spec;
        return (mapped.CppType, mapped.ParseFn, legacySpec);
    }

    public string? DescribeUserCodeProblem(string userCode) =>
        CppSolutionTypeRegex().IsMatch(userCode)
            ? null
            : "C++ code must declare the solution type the harness calls, for example "
                + "\"class Solution { public: bool isValid(string s) { ... } };\". "
                + "A bare function on its own will not compile.";


    public string Render(CodeTemplateContext context)
    {
        List<string> argDeclarations = [];
        List<string> argNames = [];
        List<ValueTypeSpec> specsForArrayParsers = [];

        for (int i = 0; i < context.Inputs.Count; i++)
        {
            (string cppType, string parseFn, ValueTypeSpec? spec) = ResolveCppType(context.Inputs[i]);

            if (spec is not null)
                specsForArrayParsers.Add(spec);

            string argName = $"arg{i}";
            argDeclarations.Add($"    {cppType} {argName} = {parseFn}(__readLine());");
            argNames.Add(argName);
        }

        string argsSection = string.Join("\n", argDeclarations);
        string callArgs = string.Join(", ", argNames);
        string arrayParsers = CppValueTypeRenderer.RenderArrayParsers(specsForArrayParsers);

        return $$"""
            #include <bits/stdc++.h>
            using namespace std;

            {{context.UserCode}}

            static string __readLine() {
                string line;
                std::getline(cin, line);
                return line;
            }

            static string __trim(const string& raw) {
                size_t start = raw.find_first_not_of(" \t\r\n");
                if (start == string::npos) return "";
                size_t end = raw.find_last_not_of(" \t\r\n");
                return raw.substr(start, end - start + 1);
            }

            static int parseInt(const string& raw) { return stoi(__trim(raw)); }
            static double parseDouble(const string& raw) { return stod(__trim(raw)); }

            static bool parseBool(const string& raw) {
                string s = __trim(raw);
                for (char& c : s) c = static_cast<char>(tolower(c));
                return s == "true";
            }

            static string parseString(const string& raw) {
                string s = __trim(raw);
                if (s.size() >= 2 && s.front() == '"' && s.back() == '"') {
                    string inner = s.substr(1, s.size() - 2);
                    string result;
                    for (size_t i = 0; i < inner.size(); i++) {
                        if (inner[i] == '\\' && i + 1 < inner.size()) {
                            result += inner[++i];
                        } else {
                            result += inner[i];
                        }
                    }
                    return result;
                }
                return s;
            }

            {{arrayParsers}}
            static string toJson(int v) { return to_string(v); }
            static string toJson(long long v) { return to_string(v); }
            static string toJson(bool v) { return v ? "true" : "false"; }

            static string toJson(double v) {
                ostringstream oss;
                oss << setprecision(15) << v;
                return oss.str();
            }

            static string toJson(const string& v) {
                string result = "\"";
                for (char c : v) {
                    if (c == '"' || c == '\\') result += '\\';
                    result += c;
                }
                return result + "\"";
            }

            template <typename T>
            static string toJson(const vector<T>& v) {
                string result = "[";
                for (size_t i = 0; i < v.size(); i++) {
                    if (i > 0) result += ",";
                    result += toJson(v[i]);
                }
                return result + "]";
            }

            static string toOutputString(const string& v) { return v; }

            template <typename T>
            static string toOutputString(const T& v) { return toJson(v); }

            int main() {
            {{argsSection}}

                Solution solution;
                auto result = solution.{{context.FunctionName}}({{callArgs}});
                cout << toOutputString(result);
                return 0;
            }
            """;
    }
}