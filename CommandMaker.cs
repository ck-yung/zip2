using System.Collections.Immutable;

namespace zip2;

internal partial class CommandMaker
{
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
            var shortcutsThe = Helper.MainShortcuts.Value!;
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

        IEnumerable<string> ParseInto(out MyCommand myCommand)
        {
            myCommand = MyCommand.Fake;
            var NamedCommands = Helper.MainCommandMakers.Value;

            var aa = ExpandFromShortcut()
                .GroupBy((it) => NamedCommands.ContainsKey(it))
                .ToImmutableDictionary((grp) => grp.Key, (grp) => grp);
            if (aa.ContainsKey(true))
            {
                var bb = aa[true].Distinct().Take(2).ToArray();
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
                    myCommand = b2.Make();
                }
                else
                {
                    throw new MyArgumentException(
                        $"Unhandled error [mark2] is found!");
                }
            }
            if (aa.ContainsKey(false)) return aa[false];
            return Enumerable.Empty<string>();
        }

        var rtn = ParseInto(out command);
        return rtn.ToArray();
    }
}

public class MyCommand
{
    public Func<string[], bool> Invoke { get; init; }
    public MyCommand(Func<string[], bool> invoke)
    {
        Invoke = invoke;
    }

    private MyCommand(bool flag)
    {
        Invoke = (_) =>
        {
            throw new NotImplementedException("Fake command");
        };
    }

    static public readonly MyCommand Fake = new(false);
    public bool IsFake()
    {
        return Object.ReferenceEquals(Fake, this);
    }
}
