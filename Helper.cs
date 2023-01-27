using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Archives.Rar;
using System.Collections.Immutable;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static zip2.Helper;

namespace zip2;

static class Helper
{
    internal sealed class Non
    {
        public static readonly Non e = new Non();
        private Non() { }
    }

    static internal readonly string ExeName;
    static internal readonly string ExeVersion;
    static internal readonly string ExeCopyright;

    static internal Func<string, string>
        ToLocalFilename { get; private set; }
    static internal string ToStandFilename(
        string filename) => filename.Replace('\\', '/');

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

        if (Path.DirectorySeparatorChar== '/')
        {
            ToLocalFilename = (path) => path.Replace('\\', '/');
        }
        else
        {
            ToLocalFilename = (path) => path.Replace('/', '\\');
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

    internal static void PrintHelp(ImmutableArray<IOption> opts)
    {
        PrintHelp(opts, MyCommand.EmptyShortcutArrays);
    }

    internal static void PrintHelp(ImmutableArray<IOption> opts,
        ImmutableDictionary<string, string[]> shortcutArrays)
    {
        Console.WriteLine("OPTION:");
        foreach (var opt in opts)
        {
            Console.WriteLine($"  {opt.Name,12}  {opt.Shortcut,2}  {opt.Help}");
        }

        if (shortcutArrays.Any())
        {
            Console.WriteLine("SHORTCUT:");
            foreach (var scThe in shortcutArrays
                .OrderBy((it) => it.Key))
            {
                Console.Write($"  {scThe.Key}   ");
                Console.WriteLine(string.Join(" ", scThe.Value));
            }
        }
    }

    static internal R Invoke<T, R>(this IEnumerable<T> seq,
    Func<IEnumerable<T>, R> func)
    {
        return func(seq);
    }

    static internal string GetFirstDirPart(string path)
    {
        var aa = path.Split(new char[] { '/', '\\' }, 2);
        if (aa.Length == 1) return ".";
        return aa[0];
    }

    static internal Func<string, bool> ToWildPredicate(string[] args)
    {
        string ToRegexText(string text)
        {
            var regText = new System.Text.StringBuilder("^");
            regText.Append(text
                .Replace(@"\", @"\\")
                .Replace("^", @"\^")
                .Replace("$", @"\$")
                .Replace(".", @"\.")
                .Replace("?", ".")
                .Replace("*", ".*")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                ).Append('$');
            return regText.ToString();
        }

        var funcs = args.AsEnumerable()
            .Select((it) => ToRegexText(it))
            .Select((it) => new Regex(it, RegexOptions.IgnoreCase))
            .ToArray();
        if (funcs.Length == 0) return (_) => true;
        return (arg) => funcs.Any((it) => it.Match(arg).Success);
    }

    static internal string ReducePentCent(long size, long compressedSize)
    {
        if (1 > size) return " 0";
        if (compressedSize > size) return " 0";
        compressedSize = size - compressedSize;
        var ratio = 1000 * compressedSize / size;
        ratio += 5; ratio /= 10;
        if (98 < ratio) return "99";
        return $"{ratio,2}";
    }

    static internal string KiloSize(long size)
    {
        var units = new char[] { 'P', 'T', 'G', 'M', 'k', ' ' };
        string sizeToText(int ndx)
        {
            if (size < 10000) return $"{size,4}{units[ndx]}";
            size += 512; size /= 1024;
            return sizeToText(ndx - 1);
        }
        return sizeToText(units.Length - 1);
    }

    static internal IEnumerable<MyZipEntry> MyZipEntries(this ZipInputStream inpZs)
    {
        ZipEntry? entryThe;
        while (null != (entryThe = inpZs.GetNextEntry()))
        {
            yield return new MyZipEntry(entryThe, inpZs);
        }
    }

    static internal IEnumerable<MyZipEntry> MyRarEntries(this RarArchive inpRars)
    {
        foreach (var entryThe in inpRars.Entries)
        {
            yield return new MyZipEntry(entryThe);
        }
    }
}

internal class MyZipEntry
{
    static public IEnumerable<MyZipEntry> GetEntries(Stream ins, string filename)
    {
        var extThe = Path.GetExtension(My.ZipFilename).ToLower();
        return (extThe) switch
        {
            ".zip" => new ZipInputStream(ins).MyZipEntries(),
            ".rar" => RarArchive.Open(ins, new SharpCompress.Readers
            .ReaderOptions()
            { LeaveStreamOpen = true }).MyRarEntries(),
            _ => throw new MyArgumentException(
                $"'{extThe}' is unknown extension"),
        };
    }

    public string Name { get; init; }
    public bool IsCrypted { get; init; }
    public bool IsFile { get; init; }
    public long Size { get; init; }
    public long CompressedSize { get; init; }
    public DateTime DateTime { get; init; }
    public Func<Stream> OpenStream { get; init; }
    public Action<Stream> CloseStream { get; init; }
    public MyZipEntry(string name, bool isCrypted, bool isFile,
        long size, long compressedSize, DateTime dateTime)
    {
        Name = name;
        IsCrypted = isCrypted;
        IsFile = isFile;
        Size = size;
        CompressedSize = compressedSize;
        DateTime = dateTime;
        OpenStream = () => Stream.Null;
        CloseStream = (_) => { };
    }

    public MyZipEntry(ZipEntry arg, ZipInputStream stream)
    {
        Name = arg.Name;
        IsCrypted = arg.IsCrypted;
        IsFile = arg.IsFile;
        Size = arg.Size;
        CompressedSize = arg.CompressedSize;
        DateTime = arg.DateTime;
        OpenStream = () => stream;
        CloseStream = (_) => { };
    }

    public MyZipEntry(RarArchiveEntry arg)
    {
        Name = arg.Key;
        IsCrypted = arg.IsEncrypted;
        IsFile = false == arg.IsDirectory;
        Size = arg.Size;
        CompressedSize = arg.CompressedSize;
        DateTime = arg.LastModifiedTime ?? DateTime.MinValue;
        OpenStream = arg.OpenEntryStream;
        CloseStream = (it) => it.Close();
    }
}

internal class SumZipEntry
{
    public string Name { get; init; }
    public SumZipEntry(string name)
    {
        Name = name;
    }

    public SumZipEntry(string name, bool isFile)
    {
        Name = name;
        if (isFile && File.Exists(name))
        {
            FileSize = new FileInfo(name).Length;
            Name = Path.GetFileName(name) ?? ".";
        }
    }

    public bool IsCrypted { get; private set; } = false;
    public int Count { get; private set; } = 0;
    public long Size { get; private set; } = 0;
    public long FileSize { get; private set; } = 0;
    public long CompressedSize { get; private set; } = 0;
    public DateTime DateTime { get; private set; } = DateTime.MaxValue;
    public DateTime Last { get; private set; } = DateTime.MinValue;

    public override string ToString()
    {
        var rtn = new StringBuilder();
        if (My.OtherColumnOpt.Invoke(Non.e))
        {
            rtn.Append(IsCrypted ? '*' : ' ');
            var sizeThe = (FileSize > 0) ? FileSize : CompressedSize;
            rtn.Append($"{ReducePentCent(Size, sizeThe)} ");
            rtn.Append($"{KiloSize(Size)} ");
            rtn.Append($"{DateTime:yyyy-MM-dd HH:mm} ");
            rtn.Append($"- ");
            rtn.Append($"{Last:yyyy-MM-dd HH:mm} ");
        }
        rtn.Append($"{Count,5:N0} ");
        rtn.Append(Name);
        return rtn.ToString();
    }

    public SumZipEntry AddWith(MyZipEntry entryThe)
    {
        Count += 1;
        Size += entryThe.Size;
        CompressedSize += entryThe.CompressedSize;
        if (DateTime > entryThe.DateTime) DateTime = entryThe.DateTime;
        if (Last < entryThe.DateTime) Last = entryThe.DateTime;
        if (entryThe.IsCrypted) IsCrypted = true;
        return this;
    }

    public SumZipEntry AddWith(SumZipEntry other)
    {
        Count += other.Count;
        Size += other.Size;
        CompressedSize += other.CompressedSize;
        if (DateTime > other.DateTime) DateTime = other.DateTime;
        if (Last < other.DateTime) Last = other.DateTime;
        if (other.IsCrypted) IsCrypted = true;
        return this;
    }
}
