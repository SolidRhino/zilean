namespace Zilean.Shared.Features.Shell;

/// <summary>
/// Builds and renders command-line arguments for a shell command.
/// </summary>
public class ArgumentsBuilder
{
    private readonly Dictionary<string, List<string>> _arguments = [];

    /// <summary>
    /// Creates a new empty <see cref="ArgumentsBuilder"/>.
    /// </summary>
    /// <returns>A new <see cref="ArgumentsBuilder"/> instance.</returns>
    public static ArgumentsBuilder Create() => new();

    /// <summary>
    /// Removes all arguments from the builder.
    /// </summary>
    /// <returns>This builder instance for chaining.</returns>
    public ArgumentsBuilder Clear()
    {
        _arguments.Clear();

        return this;
    }


    /// <summary>
    /// Appends an argument with an associated value to the builder.
    /// </summary>
    /// <param name="argument">The argument key (e.g. <c>--flag</c>).</param>
    /// <param name="newValue">The value for the argument.</param>
    /// <param name="allowDuplicates">Whether to allow multiple values for the same argument key.</param>
    /// <param name="quoteValue">Whether to wrap the value in double quotes.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ArgumentsBuilder AppendArgument(string argument, string newValue, bool allowDuplicates = false, bool quoteValue = true)
    {
        if (!_arguments.TryGetValue(argument, out var value))
        {
            value = quoteValue ? [$"\"{newValue}\""] : [newValue];
            _arguments[argument] = value;

            return this;
        }

        if (allowDuplicates)
        {
            value.Add(quoteValue ? $"\"{newValue}\"" : newValue);
        }

        return this;
    }

    /// <summary>
    /// Renders the accumulated arguments into a single command-line string.
    /// </summary>
    /// <param name="propertyKeySeparator">The character used to separate argument keys from their values.</param>
    /// <returns>A space-delimited string of all arguments.</returns>
    public string RenderArguments(char propertyKeySeparator = ' ')
    {
        var renderedArguments = new List<string>();

        foreach (var arg in _arguments)
        {
            foreach (var value in arg.Value)
            {
                if (value == string.Empty)
                {
                    renderedArguments.Add(arg.Key);
                    continue;
                }

                if (arg.Key.StartsWith("-p"))
                {
                    renderedArguments.Add($"{arg.Key}={value}");
                    continue;
                }

                renderedArguments.Add($"{arg.Key}{propertyKeySeparator}{value}");
            }
        }

        return string.Join(" ", renderedArguments);
    }

}