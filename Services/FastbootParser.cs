namespace TechFixStudio.Services;

public static class FastbootParser
{
    public static Dictionary<string, string> Parse(string raw)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var rawLine in raw.Split(
                     new[] { '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var index = line.IndexOf(':');

            if (index <= 0)
            {
                continue;
            }

            var key = line[..index].Trim();

            var value = line[(index + 1)..].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }

    public static string Get(
        Dictionary<string, string> values,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }

    public static bool IsTrue(string value)
    {
        return value.Equals(
                   "yes",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "true",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "1",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "unlocked",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFalse(string value)
    {
        return value.Equals(
                   "no",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "false",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "0",
                   StringComparison.OrdinalIgnoreCase)
               || value.Equals(
                   "locked",
                   StringComparison.OrdinalIgnoreCase);
    }
}
