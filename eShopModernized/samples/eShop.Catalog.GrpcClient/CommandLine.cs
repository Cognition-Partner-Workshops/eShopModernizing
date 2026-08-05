using System.Diagnostics.CodeAnalysis;

namespace eShop.Catalog.GrpcClient;

/// <summary>
/// A parsed command line: the endpoint to talk to, the canonical verb and the positional
/// arguments that follow it. Parsing is separated from the RPC calls so it can be tested without
/// a running service.
/// </summary>
public sealed class CommandLine
{
    public const string DefaultAddress = "http://localhost:5095";

    /// <summary>Verb printed by <c>--help</c> and used when the command line carries no verb.</summary>
    public const string HelpVerb = "help";

    /// <summary>
    /// Canonical verb per alias. The canonical names mirror the legacy WCF operations
    /// (<c>get-brands</c> ↔ <c>GetCatalogBrands</c>); the short forms are kept so the command
    /// lines published with the first version of this sample keep working.
    /// </summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["brands"] = "get-brands",
        ["types"] = "get-types",
        ["items"] = "get-items",
        ["find"] = "find-item",
        ["create"] = "create-item",
        ["update"] = "update-item",
        ["remove"] = "remove-item",
        ["stock"] = "get-stock",
        ["add-stock"] = "create-stock",
        ["discount"] = "get-discount",
        ["demo"] = "catalog",
        ["-h"] = HelpVerb,
        ["--help"] = HelpVerb,
        ["-?"] = HelpVerb,
    };

    private readonly IReadOnlyList<string> _arguments;

    private CommandLine(string address, string verb, IReadOnlyList<string> arguments)
    {
        Address = address;
        Verb = verb;
        _arguments = arguments;
    }

    /// <summary>The gRPC endpoint, from <c>--address</c> or <see cref="DefaultAddress" />.</summary>
    public string Address { get; }

    /// <summary>The canonical verb; <see cref="HelpVerb" /> when no verb was supplied.</summary>
    public string Verb { get; }

    /// <summary>The positional arguments after the verb.</summary>
    public IReadOnlyList<string> Arguments => _arguments;

    /// <summary>True when the command line carried no verb at all, which is a usage error.</summary>
    public bool IsEmpty { get; private init; }

    public static CommandLine Parse(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var remaining = new List<string>(args);
        var address = TakeOption(remaining, "--address") ?? DefaultAddress;

        if (remaining.Count == 0)
        {
            return new CommandLine(address, HelpVerb, Array.Empty<string>()) { IsEmpty = true };
        }

        var verb = Canonicalize(remaining[0]);
        return new CommandLine(address, verb, remaining.GetRange(1, remaining.Count - 1));
    }

    /// <summary>Resolves an alias to its canonical verb; unknown verbs are returned unchanged.</summary>
    public static string Canonicalize(string verb)
        => Aliases.TryGetValue(verb, out var canonical) ? canonical : verb;

    /// <summary>The positional argument at <paramref name="index" />, or null when absent.</summary>
    public string? Optional(int index) => index < _arguments.Count ? _arguments[index] : null;

    /// <summary>
    /// The positional argument at <paramref name="index" />, or a <see cref="CommandLineException" />
    /// naming the missing argument.
    /// </summary>
    public string Required(int index, string name)
        => Optional(index) ?? throw new CommandLineException($"Missing argument '{name}'.");

    /// <summary>Removes <paramref name="name" /> and its value from the argument list.</summary>
    private static string? TakeOption(List<string> arguments, string name)
    {
        var index = arguments.IndexOf(name);
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= arguments.Count)
        {
            throw new CommandLineException($"Option '{name}' needs a value.");
        }

        var value = arguments[index + 1];
        arguments.RemoveRange(index, 2);
        return value;
    }
}

/// <summary>A command line the user can fix: a missing argument, an unparsable value, a bad verb.</summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "The parser only ever raises this with a message.")]
public sealed class CommandLineException : Exception
{
    public CommandLineException(string message)
        : base(message)
    {
    }

    public CommandLineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
