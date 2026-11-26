using System.Text;

namespace StackDuel.Data.Infrastructure;

internal static class LogTemplate
{
    public static string Render(string template, object[] args)
    {
        if (args.Length == 0 || !template.Contains('{', StringComparison.Ordinal))
            return template;

        StringBuilder rendered = new(template.Length + 32);
        int argIndex = 0;
        int position = 0;

        while (position < template.Length)
        {
            int open = template.IndexOf('{', position);
            int close = open < 0 ? -1 : template.IndexOf('}', open + 1);

            if (close < 0)
                break;

            rendered.Append(template, position, open - position);
            rendered.Append(argIndex < args.Length ? args[argIndex]?.ToString() : template[open..(close + 1)]);

            argIndex++;
            position = close + 1;
        }

        rendered.Append(template, position, template.Length - position);

        return rendered.ToString();
    }
}