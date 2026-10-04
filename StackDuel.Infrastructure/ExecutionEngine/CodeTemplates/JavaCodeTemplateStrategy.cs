using StackDuel.Application.ExecutionEngine;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal sealed partial class JavaCodeTemplateStrategy : ICodeTemplateStrategy
{
    private static readonly Dictionary<string, (string JavaType, string ParseMethod)> TypesByValueType = new()
    {
        ["integer"] = ("int", "parseInt"),
        ["double"] = ("double", "parseDouble"),
        ["boolean"] = ("boolean", "parseBoolean"),
        ["string"] = ("String", "parseString"),
        ["integer_array"] = ("int[]", "parseIntArray"),
    };

    private static readonly Dictionary<string, string> CommonImportsByClassName = new()
    {
        ["List"] = "java.util.List",
        ["ArrayList"] = "java.util.ArrayList",
        ["LinkedList"] = "java.util.LinkedList",
        ["Map"] = "java.util.Map",
        ["HashMap"] = "java.util.HashMap",
        ["TreeMap"] = "java.util.TreeMap",
        ["LinkedHashMap"] = "java.util.LinkedHashMap",
        ["Set"] = "java.util.Set",
        ["HashSet"] = "java.util.HashSet",
        ["TreeSet"] = "java.util.TreeSet",
        ["LinkedHashSet"] = "java.util.LinkedHashSet",
        ["Deque"] = "java.util.Deque",
        ["ArrayDeque"] = "java.util.ArrayDeque",
        ["Queue"] = "java.util.Queue",
        ["PriorityQueue"] = "java.util.PriorityQueue",
        ["Stack"] = "java.util.Stack",
        ["Collections"] = "java.util.Collections",
        ["Arrays"] = "java.util.Arrays",
        ["Comparator"] = "java.util.Comparator",
        ["Iterator"] = "java.util.Iterator",
        ["Optional"] = "java.util.Optional",
        ["Scanner"] = "java.util.Scanner",
    };

    public string LanguageName => "Java";

    public string Render(CodeTemplateContext context) => string.Empty;

    public string BuildStdin(IReadOnlyList<CodeTemplateInput> inputs) => string.Join("\n", inputs.Select(i => i.Value));

    public byte[]? BuildAdditionalFiles(CodeTemplateContext context)
    {
        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "Solution.java", EnsureCommonImports(context.UserCode));
            AddEntry(archive, "Main.java", RenderMain(context));
            AddEntry(archive, "compile", "#!/bin/bash\n/usr/local/jdk17/bin/javac *.java\n");
            AddEntry(archive, "run", "#!/bin/bash\n/usr/local/jdk17/bin/java Main\n");
        }

        return stream.ToArray();
    }

    private static string EnsureCommonImports(string sourceCode)
    {
        HashSet<string> importedTypes = [.. ImportLineRegex().Matches(sourceCode).Select(m => m.Groups[1].Value)];

        if (importedTypes.Contains("java.util.*"))
            return sourceCode;

        List<string> missingImports = [];

        foreach ((string className, string fullyQualifiedName) in CommonImportsByClassName)
        {
            if (importedTypes.Contains(fullyQualifiedName))
                continue;

            if (Regex.IsMatch(sourceCode, $@"\b{Regex.Escape(className)}\b", RegexOptions.None, TimeSpan.FromSeconds(1)))
                missingImports.Add(fullyQualifiedName);
        }

        if (missingImports.Count == 0)
            return sourceCode;

        string importBlock = string.Join("\n", missingImports.Select(i => $"import {i};"));
        return $"{importBlock}\n\n{sourceCode}";
    }

    [GeneratedRegex(@"^\s*import\s+([\w.*]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex ImportLineRegex();

    private static string RenderMain(CodeTemplateContext context)
    {
        List<string> argDeclarations = [];
        List<string> argNames = [];

        for (int i = 0; i < context.Inputs.Count; i++)
        {
            string valueType = context.Inputs[i].ValueType;

            if (!TypesByValueType.TryGetValue(valueType, out (string JavaType, string ParseMethod) mapped))
                throw new NotSupportedException($"Unsupported Java value type '{valueType}'.");

            string argName = $"arg{i}";
            argDeclarations.Add($"        {mapped.JavaType} {argName} = {mapped.ParseMethod}(__in.readLine());");
            argNames.Add(argName);
        }

        string argsSection = string.Join("\n", argDeclarations);
        string callArgs = string.Join(", ", argNames);

        return $$"""
            import java.io.BufferedReader;
            import java.io.InputStreamReader;
            import java.util.List;

            public class Main {
                public static void main(String[] args) throws Exception {
                    BufferedReader __in = new BufferedReader(new InputStreamReader(System.in));
            {{argsSection}}

                    Solution solution = new Solution();
                    var result = solution.{{context.FunctionName}}({{callArgs}});
                    System.out.print(toOutputString(result));
                }

                static int parseInt(String raw) {
                    return Integer.parseInt(raw.trim());
                }

                static double parseDouble(String raw) {
                    return Double.parseDouble(raw.trim());
                }

                static boolean parseBoolean(String raw) {
                    return Boolean.parseBoolean(raw.trim());
                }

                static String parseString(String raw) {
                    String s = raw == null ? "" : raw.trim();
                    if (s.length() >= 2 && s.charAt(0) == '"' && s.charAt(s.length() - 1) == '"') {
                        return s.substring(1, s.length() - 1).replace("\\\"", "\"").replace("\\\\", "\\");
                    }
                    return s;
                }

                static int[] parseIntArray(String raw) {
                    String s = raw.trim();
                    s = s.substring(1, s.length() - 1).trim();
                    if (s.isEmpty()) {
                        return new int[0];
                    }
                    String[] parts = s.split(",");
                    int[] result = new int[parts.length];
                    for (int i = 0; i < parts.length; i++) {
                        result[i] = Integer.parseInt(parts[i].trim());
                    }
                    return result;
                }

                static String toOutputString(Object value) {
                    if (value instanceof String s) {
                        return s;
                    }
                    return toJson(value);
                }

                static String toJson(Object value) {
                    if (value == null) {
                        return "null";
                    }
                    if (value instanceof String s) {
                        return "\"" + s.replace("\\", "\\\\").replace("\"", "\\\"") + "\"";
                    }
                    if (value instanceof Boolean || value instanceof Integer || value instanceof Long
                            || value instanceof Double || value instanceof Float || value instanceof Short) {
                        return String.valueOf(value);
                    }
                    if (value instanceof int[] a) {
                        StringBuilder sb = new StringBuilder("[");
                        for (int i = 0; i < a.length; i++) {
                            if (i > 0) {
                                sb.append(",");
                            }
                            sb.append(a[i]);
                        }
                        return sb.append("]").toString();
                    }
                    if (value instanceof double[] a) {
                        StringBuilder sb = new StringBuilder("[");
                        for (int i = 0; i < a.length; i++) {
                            if (i > 0) {
                                sb.append(",");
                            }
                            sb.append(a[i]);
                        }
                        return sb.append("]").toString();
                    }
                    if (value instanceof boolean[] a) {
                        StringBuilder sb = new StringBuilder("[");
                        for (int i = 0; i < a.length; i++) {
                            if (i > 0) {
                                sb.append(",");
                            }
                            sb.append(a[i]);
                        }
                        return sb.append("]").toString();
                    }
                    if (value instanceof Object[] a) {
                        StringBuilder sb = new StringBuilder("[");
                        for (int i = 0; i < a.length; i++) {
                            if (i > 0) {
                                sb.append(",");
                            }
                            sb.append(toJson(a[i]));
                        }
                        return sb.append("]").toString();
                    }
                    if (value instanceof List<?> list) {
                        StringBuilder sb = new StringBuilder("[");
                        for (int i = 0; i < list.size(); i++) {
                            if (i > 0) {
                                sb.append(",");
                            }
                            sb.append(toJson(list.get(i)));
                        }
                        return sb.append("]").toString();
                    }
                    throw new RuntimeException("Unsupported return type: " + value.getClass());
                }
            }
            """;
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using Stream entryStream = entry.Open();
        using StreamWriter writer = new(entryStream, new UTF8Encoding(false));
        writer.Write(content);
    }
}