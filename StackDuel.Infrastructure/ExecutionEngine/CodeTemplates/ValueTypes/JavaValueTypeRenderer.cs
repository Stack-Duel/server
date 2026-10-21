using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates.ValueTypes;

internal static class JavaValueTypeRenderer
{
    public static IReadOnlySet<ValueTypeKind> SupportedKinds { get; } =
        new HashSet<ValueTypeKind>
        {
            ValueTypeKind.Integer,
            ValueTypeKind.Double,
            ValueTypeKind.Boolean,
            ValueTypeKind.String,
            ValueTypeKind.Array,
        };

    public static string RenderType(ValueTypeSpec spec) =>
        spec.Kind switch
        {
            ValueTypeKind.Integer => "int",
            ValueTypeKind.Double => "double",
            ValueTypeKind.Boolean => "boolean",
            ValueTypeKind.String => "String",
            ValueTypeKind.Array => $"{RenderType(ElementOf(spec))}[]",
            _ => throw new NotSupportedException($"Unsupported Java value type kind '{spec.Kind}'."),
        };

    public static string ParserName(ValueTypeSpec spec) => $"parse{Mangle(spec)}";

    public static string RenderArrayParsers(IEnumerable<ValueTypeSpec> specs)
    {
        Dictionary<string, ValueTypeSpec> arraySpecs = [];

        foreach (ValueTypeSpec spec in specs)
            CollectArraySpecs(spec, arraySpecs);

        if (arraySpecs.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        builder.Append(ElementSplitter);

        foreach (ValueTypeSpec spec in arraySpecs.OrderBy(entry => entry.Key).Select(entry => entry.Value))
        {
            builder.AppendLine();
            builder.Append(RenderArrayParser(spec));
        }

        return builder.ToString();
    }

    private static void CollectArraySpecs(ValueTypeSpec spec, Dictionary<string, ValueTypeSpec> collected)
    {
        if (spec.Kind != ValueTypeKind.Array)
            return;

        CollectArraySpecs(ElementOf(spec), collected);
        collected.TryAdd(Mangle(spec), spec);
    }

    private static string RenderArrayParser(ValueTypeSpec spec)
    {
        ValueTypeSpec element = ElementOf(spec);
        string resultType = RenderType(spec);
        string elementBrackets = string.Concat(Enumerable.Repeat("[]", ArrayDepth(element)));
        string creation = $"new {ScalarBase(element)}[parts.length]{elementBrackets}";

        return $$"""
                static {{resultType}} {{ParserName(spec)}}(String raw) {
                    String[] parts = __splitTopLevel(raw);
                    {{resultType}} result = {{creation}};
                    for (int i = 0; i < parts.length; i++) {
                        result[i] = {{ParserName(element)}}(parts[i]);
                    }
                    return result;
                }

        """;
    }

    private static string Mangle(ValueTypeSpec spec) =>
        spec.Kind switch
        {
            ValueTypeKind.Integer => "Int",
            ValueTypeKind.Double => "Double",
            ValueTypeKind.Boolean => "Boolean",
            ValueTypeKind.String => "String",
            ValueTypeKind.Array => $"{Mangle(ElementOf(spec))}Array",
            _ => throw new NotSupportedException($"Unsupported Java value type kind '{spec.Kind}'."),
        };

    private static string ScalarBase(ValueTypeSpec spec) =>
        spec.Kind == ValueTypeKind.Array ? ScalarBase(ElementOf(spec)) : RenderType(spec);

    private static int ArrayDepth(ValueTypeSpec spec) =>
        spec.Kind == ValueTypeKind.Array ? ArrayDepth(ElementOf(spec)) + 1 : 0;

    private static ValueTypeSpec ElementOf(ValueTypeSpec spec) =>
        spec.Element ?? throw new NotSupportedException("An array value type requires an element type.");

    private const string ElementSplitter = """
                static String[] __splitTopLevel(String raw) {
                    String s = raw == null ? "" : raw.trim();
                    if (s.length() >= 2 && s.charAt(0) == '[' && s.charAt(s.length() - 1) == ']') {
                        s = s.substring(1, s.length() - 1).trim();
                    }
                    if (s.isEmpty()) {
                        return new String[0];
                    }
                    java.util.List<String> parts = new java.util.ArrayList<>();
                    StringBuilder current = new StringBuilder();
                    int depth = 0;
                    boolean inString = false;
                    boolean escaped = false;
                    for (int i = 0; i < s.length(); i++) {
                        char c = s.charAt(i);
                        if (escaped) {
                            current.append(c);
                            escaped = false;
                        } else if (c == '\\') {
                            current.append(c);
                            escaped = true;
                        } else if (c == '"') {
                            inString = !inString;
                            current.append(c);
                        } else if (!inString && (c == '[' || c == '{')) {
                            depth++;
                            current.append(c);
                        } else if (!inString && (c == ']' || c == '}')) {
                            depth--;
                            current.append(c);
                        } else if (!inString && c == ',' && depth == 0) {
                            parts.add(current.toString());
                            current.setLength(0);
                        } else {
                            current.append(c);
                        }
                    }
                    parts.add(current.toString());
                    return parts.toArray(new String[0]);
                }

        """;
}