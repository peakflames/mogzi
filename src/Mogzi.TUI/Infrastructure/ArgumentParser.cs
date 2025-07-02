namespace Mogzi.TUI.Infrastructure;

/// <summary>
/// Simple argument parser for AOT compatibility.
/// </summary>
public static class ArgumentParser
{
    /// <summary>
    /// Parses command line arguments into a dictionary.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Dictionary of parsed arguments.</returns>
    public static Dictionary<string, string?> Parse(string[] args)
    {
        var result = new Dictionary<string, string?>();
        var positionalArgs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--"))
            {
                // Long option
                var key = arg[2..];
                if (key.Contains('='))
                {
                    var parts = key.Split('=', 2);
                    result[parts[0]] = parts[1];
                }
                else
                {
                    result[key] = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[++i] : "true";
                }
            }
            else if (arg.StartsWith("-") && arg.Length > 1)
            {
                // Short option
                var key = arg[1..];
                result[key] = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[++i] : "true";
            }
            else
            {
                // Positional argument
                positionalArgs.Add(arg);
            }
        }

        // Add positional arguments with numeric keys
        for (var i = 0; i < positionalArgs.Count; i++)
        {
            result[$"_{i}"] = positionalArgs[i];
        }

        return result;
    }

    /// <summary>
    /// Gets a string value from parsed arguments.
    /// </summary>
    public static string? GetString(Dictionary<string, string?> args, string key, string? defaultValue = null)
    {
        return args.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets a string value from parsed arguments, checking multiple possible keys (for aliases).
    /// </summary>
    public static string? GetString(Dictionary<string, string?> args, string[] keys, string? defaultValue = null)
    {
        foreach (var key in keys)
        {
            if (args.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets a boolean value from parsed arguments.
    /// </summary>
    public static bool GetBool(Dictionary<string, string?> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value?.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Checks if an argument exists.
    /// </summary>
    public static bool HasFlag(Dictionary<string, string?> args, string key)
    {
        return args.ContainsKey(key);
    }
}
