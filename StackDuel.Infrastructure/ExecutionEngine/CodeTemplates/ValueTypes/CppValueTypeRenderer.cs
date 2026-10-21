using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates.ValueTypes;

internal static class CppValueTypeRenderer
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
            ValueTypeKind.Boolean => "bool",
            ValueTypeKind.String => "string",
            ValueTypeKind.Array => $"vector<{RenderType(ElementOf(spec))}>",
            _ => throw new NotSupportedException($"Unsupported C++ value type kind '{spec.Kind}'."),
        };

    public static string ParserName(ValueTypeSpec spec) => $"parse{Mangle(spec)}";

    public static string RenderArrayParsers(IEnumerable<ValueTypeSpec> specs)
    {
        List<ValueTypeSpec> arraySpecs = [];
        HashSet<string> seen = [];

        foreach (ValueTypeSpec spec in specs)
            CollectArraySpecs(spec, arraySpecs, seen);

        if (arraySpecs.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        builder.Append(ElementSplitter);

        foreach (ValueTypeSpec spec in arraySpecs)
        {
            builder.AppendLine();
            builder.Append(RenderArrayParser(spec));
        }

        return builder.ToString();
    }

    private static void CollectArraySpecs(ValueTypeSpec spec, List<ValueTypeSpec> collected, HashSet<string> seen)
    {
        if (spec.Kind != ValueTypeKind.Array)
            return;

        CollectArraySpecs(ElementOf(spec), collected, seen);

        if (seen.Add(Mangle(spec)))
            collected.Add(spec);
    }

    private static string RenderArrayParser(ValueTypeSpec spec)
    {
        ValueTypeSpec element = ElementOf(spec);
        string resultType = RenderType(spec);

        return $$"""
            static {{resultType}} {{ParserName(spec)}}(const string& raw) {
                vector<string> parts = __splitTopLevel(raw);
                {{resultType}} result;
                result.reserve(parts.size());
                for (const string& part : parts) {
                    result.push_back({{ParserName(element)}}(part));
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
            ValueTypeKind.Boolean => "Bool",
            ValueTypeKind.String => "String",
            ValueTypeKind.Array => $"{Mangle(ElementOf(spec))}Array",
            _ => throw new NotSupportedException($"Unsupported C++ value type kind '{spec.Kind}'."),
        };

    private static ValueTypeSpec ElementOf(ValueTypeSpec spec) =>
        spec.Element ?? throw new NotSupportedException("An array value type requires an element type.");

    private const string ElementSplitter = """
        static vector<string> __splitTopLevel(const string& raw) {
            string s = __trim(raw);
            if (s.size() >= 2 && s.front() == '[' && s.back() == ']') {
                s = __trim(s.substr(1, s.size() - 2));
            }
            vector<string> parts;
            if (s.empty()) {
                return parts;
            }
            string current;
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            for (size_t i = 0; i < s.size(); i++) {
                char c = s[i];
                if (escaped) {
                    current += c;
                    escaped = false;
                } else if (c == '\\') {
                    current += c;
                    escaped = true;
                } else if (c == '"') {
                    inString = !inString;
                    current += c;
                } else if (!inString && (c == '[' || c == '{')) {
                    depth++;
                    current += c;
                } else if (!inString && (c == ']' || c == '}')) {
                    depth--;
                    current += c;
                } else if (!inString && c == ',' && depth == 0) {
                    parts.push_back(current);
                    current.clear();
                } else {
                    current += c;
                }
            }
            parts.push_back(current);
            return parts;
        }

        """;
}