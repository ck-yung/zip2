using System.Collections.Immutable;
using System.Reflection;

namespace zip2;
static class Helper
{
    static internal readonly string ExeName;
    static internal readonly string ExeVersion;
    static internal readonly string ExeCopyright;

    static Helper()
    {
        var asm = Assembly.GetExecutingAssembly();
        var asmName = asm.GetName();
        ExeName = asmName.Name ?? "?";
        ExeVersion = asmName.Version?.ToString() ?? "?";
        var aa = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute),
            inherit: false);
        if (aa.Length > 0)
        {
            ExeCopyright = ((AssemblyCopyrightAttribute)aa[0]).Copyright;
        }
        else
        {
            ExeCopyright = "?";
        }
    }

    static public string GetExeEnvr()
    {
        return Environment.GetEnvironmentVariable(ExeName) ?? string.Empty;
    }

    static public IEnumerable<(CommandAttribute, Type)> GetMainCommands()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .AsEnumerable()
            .Select((it) => (it.GetCustomAttributes(
                typeof(CommandAttribute), inherit: false), it))
            .Where((it) => it.Item1.Length > 0)
            .Select((it) => (it.Item1.Cast<CommandAttribute>().First(), it.Item2));
    }

    static internal Lazy<ImmutableDictionary<string, string>> MainShortcuts
        = new Lazy<ImmutableDictionary<string, string>>(() =>
        GetMainCommands()
        .Where((it) => it.Item1.Shortcut.Length > 0)
        .ToImmutableDictionary((it) => it.Item1.Shortcut, (it2) => it2.Item1.Name));

    static internal Lazy<ImmutableDictionary<string, Type>> MainCommandMakers
        = new Lazy<ImmutableDictionary<string, Type>>(() =>
        GetMainCommands()
        .Where((it) => it.Item1.Name.Length > 0)
        .ToImmutableDictionary((it) => it.Item1.Name, (it2) => it2.Item2));
}
