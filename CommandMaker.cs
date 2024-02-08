using System.Collections.Immutable;

namespace zip2;

internal partial class CommandMaker
{
    public const string HelpText = "--help";
    public string Shortcut { get; init; }
    public string Name { get; init; }
    public Func<MyCommand> Create { get; init; }
    public CommandMaker(string shortcut, string name,
        Func<MyCommand> create)
    {
        Console.WriteLine($"Creating CommandMaker '{name}'");
        Shortcut = shortcut;
        Name = name;
        Create = create;
    }

    static public string[] Parse(string[] args, out MyCommand command)
    {
        IEnumerable<string> ExpandCombiningShortcut()
        {
            var it = args.AsEnumerable().GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (current.Length < 3)
                {
                    yield return current;
                }
                else if (current.StartsWith("--"))
                {
                    yield return current;
                }
                else if (current[0] != '-')
                {
                    yield return current;
                }
                else
                {
                    foreach (var charThe in current.Substring(1))
                    {
                        yield return $"-{charThe}";
                    }
                }
            }
        }

        IEnumerable<string> ExpandFromShortcut()
        {
            var shortcutsThe = Helper.MainShortcuts.Value;
            var it = ExpandCombiningShortcut().GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (shortcutsThe.TryGetValue(current, out string? value))
                {
                    yield return value;
                }
                else
                {
                    yield return current;
                }
            }
        }

        command = MyCommand.Fake;
        var NamedCommands = Helper.MainCommandMakers.Value;

        var aa = ExpandFromShortcut()
            .GroupBy((it) => NamedCommands.ContainsKey(it))
            .ToImmutableDictionary((grp) => grp.Key, (grp) => grp);
        if (aa.TryGetValue(true, out var aa2))
        {
            var bb = aa2.Distinct().Take(3).ToArray();
            if (bb.Length == 2)
            {
                int cmdFound = -1;
                if (bb[0] == HelpText) cmdFound = 1;
                else if (bb[1] == HelpText) cmdFound = 0;
                if (cmdFound > -1)
                {
                    if (Activator.CreateInstance(NamedCommands[bb[cmdFound]])
                        is ICommandMaker b3)
                    {
                        command = b3.Make();
                    }
                    return [HelpText];
                }
            }

            if (bb.Length > 1)
            {
                throw new MyArgumentException(
                    $"Too many commands ({bb[0]}, {bb[1]}) are found!");
            }

            if (bb.Length != 1)
            {
                throw new MyArgumentException(
                    $"Unhandled error [mark1] is found!");
            }

            if (Activator.CreateInstance(NamedCommands[bb[0]])
                is ICommandMaker b2)
            {
                command = b2.Make();
            }
            else
            {
                throw new MyArgumentException(
                    $"Unhandled error [mark2] is found!");
            }
        }

        if (aa.TryGetValue(false, out var bb2))
        {
            var rtn= bb2.ToArray();
            return rtn;
        }
        return [];
    }
}

public class MyCommand
{
    static internal readonly ImmutableDictionary<string, string> EmptyShortcuts
        = ImmutableDictionary<string, string>.Empty;
    static internal readonly ImmutableDictionary<string, string[]> EmptyShortcutArrays
        = ImmutableDictionary<string, string[]>.Empty;

    public Func<string[], bool> Invoke { get; init; }
    readonly ImmutableDictionary<string, string[]> ShortcutArrays;
    public ImmutableArray<IOption> Options { get; init; }

    public IEnumerable<(bool, string)> Parse(IEnumerable<(bool, string)> mainArgs)
    {
        var shortcuts = Options
            .Where((it) => it.Shortcut.Length > 0)
            .ToImmutableDictionary((it) => it.Shortcut, (it) => it.Name);
        IEnumerable<(bool, string)> ExpandShortcuts()
        {
            var it = mainArgs.GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (shortcuts.TryGetValue(current.Item2,
                    out string? value))
                {
                    yield return (current.Item1, value);
                }
                else if (ShortcutArrays.TryGetValue(current.Item2,
                    out string[]? values))
                {
                    foreach (var value2 in values)
                    {
                        yield return (current.Item1, value2);
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        var args = ExpandShortcuts();
        foreach (var opt in Options)
        {
            var mapThe = opt.Parse(args)
                .GroupBy((it) => it.Item1)
                .ToImmutableDictionary((grp) => grp.Key, (grp) => grp);

            if (mapThe.ContainsKey(true))
            {
                opt.Resolve(opt, mapThe[true]
                    .Select((it) => it.Item2)
                    .Where((it) => it.Length > 0)
                    .Distinct());
            }

            if (mapThe.ContainsKey(false))
            {
                args = mapThe[false];
            }
            else
            {
                return Array.Empty<(bool, string)>();
            }
        }
        return args;
    }

    public MyCommand(Func<string[], bool> invoke)
    {
        Invoke = invoke;
        ShortcutArrays = EmptyShortcutArrays;
        Options = Array.Empty<IOption>().ToImmutableArray();
    }

    public MyCommand(Func<string[], bool> invoke,
        ImmutableArray<IOption> options)
    {
        Invoke = invoke;
        ShortcutArrays = EmptyShortcutArrays;
        Options = options;
    }

    public MyCommand(Func<string[], bool> invoke,
        ImmutableArray<IOption> options,
        ImmutableDictionary<string, string[]> shortcutArrays)
    {
        Invoke = invoke;
        ShortcutArrays = shortcutArrays;
        Options = options;
    }

    public MyCommand(Func<string[], bool> invoke,
        ImmutableDictionary<string, string[]> shortcutArrays)
    {
        Invoke = invoke;
        ShortcutArrays = shortcutArrays;
        Options = Array.Empty<IOption>().ToImmutableArray();
    }

    private MyCommand(bool flag)
    {
        Invoke = (_) =>
        {
            throw new NotImplementedException("Fake command");
        };
        ShortcutArrays = EmptyShortcutArrays;
        Options = Array.Empty<IOption>().ToImmutableArray();
    }

    static public readonly MyCommand Fake = new(false);
    public bool IsFake()
    {
        return Object.ReferenceEquals(Fake, this);
    }
}
