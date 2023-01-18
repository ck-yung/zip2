using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;
using System.Text;
using static zip2.Helper;
namespace zip2;

static internal partial class My
{
    static internal IInvokeOption<Non, bool> OtherColumnOpt
        = new NoValueOption<Non, bool>("--name-only",
            init: (_) => true, alt: (_) => false);
}


[Command(name: "--list", shortcut: "-t", help: """
      zip2 -tf ZIP-FILE [OPTION ..] [WILD ..]
    """)]
public class List : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static readonly ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.Verbose,
        (IOption) My.OtherColumnOpt,
        (IOption) My.TotalText,
        (IOption) My.ExclFiles,
        (IOption) My.SumZip,
        (IOption) My.FilesFrom,
    }.ToImmutableArray();

    static readonly ImmutableDictionary<string, string[]> MyShortcuts =
        new Dictionary<string, string[]>
        {
            ["-b"] = new string[] {
                "--verbose", "--name-only", "--total-off"},
        }.ToImmutableDictionary();
    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => List.Invoke(args),
            options: MyOptions,
            shortcutArrays: MyShortcuts)
        { }
    }

    static internal IEnumerable<ZipEntry> GetEntries(ZipInputStream inpZs)
    {
        ZipEntry? entryThe;
        while (null != (entryThe = inpZs.GetNextEntry()))
        {
            yield return entryThe;
        }
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
            return sizeToText(ndx-1);
        }
        return sizeToText(units.Length - 1);
    }

    static bool Invoke(string[] args)
    {
        if (args.Contains(CommandMaker.HelpText))
        {
            Console.WriteLine(
                """
                List zip file:
                  zip2 -tf ZIP-FILE [OPTION ..] [WILD ..]
                """);
            Helper.PrintHelp(MyOptions,
                shortcutArrays: MyShortcuts);
            return false;
        }

        var wildNames = Helper.ToWildPredicate(args);

        var namesFromFile = My.FilesFrom.Invoke(true)
            .Where((it) => it.Length > 0)
            .Distinct()
            .Select((it) => Helper.ToStandFilename(it))
            .ToArray();

        Func<ZipEntry, bool> checkZipEntryName =
            (args.Length, namesFromFile.Length) switch
            {
                (0, 0) => (_) => true,
                (> 0, 0) => (it) => wildNames(Path.GetFileName(it.Name)),
                (0, > 0) => (it) => namesFromFile.Contains(it.Name),

                _ => (it) => namesFromFile.Contains(it.Name) ||
                wildNames(Path.GetFileName(it.Name)),
            };

        var ins = My.OpenZip.Invoke((true, "'zip2 -t?' for help"));
        if (ins == Stream.Null)
        {
            Console.Error.WriteLine($"Open {My.ZipFilename} failed.");
            return false;
        }

        var inpZs = new ZipInputStream(ins);
        var sumThe = GetEntries(inpZs)
            .Where((it) => checkZipEntryName(it))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Invoke(My.SumZip.Invoke);
        ins.Close();
        My.TotalText.Invoke(sumThe.ToString());
        return true;
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
            rtn.Append($"{List.ReducePentCent(Size, sizeThe)} ");
            rtn.Append($"{List.KiloSize(Size)} ");
            rtn.Append($"{DateTime:yyyy-MM-dd HH:mm} ");
            rtn.Append($"- ");
            rtn.Append($"{Last:yyyy-MM-dd HH:mm} ");
        }
        rtn.Append($"{Count,5:N0} ");
        rtn.Append(Name);
        return rtn.ToString();
    }

    public SumZipEntry AddWith(ZipEntry entryThe)
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

static internal partial class My
{
    static internal IInvokeOption<IEnumerable<ZipEntry>, SumZipEntry> SumZip
        = new SingleValueOption<IEnumerable<ZipEntry>, SumZipEntry>(
            "--sum", help: "ext | dir",
            init: (seq) => seq
            .Select((it) =>
            {
                if (My.OtherColumnOpt.Invoke(Non.e))
                {
                    var textThe = new StringBuilder();
                    textThe.Append(it.IsCrypted ? '*' : ' ');
                    textThe.Append(
                        $"{List.ReducePentCent(it.Size, it.CompressedSize)} ");
                    textThe.Append($"{List.KiloSize(it.Size)} ");
                    textThe.Append($"{it.DateTime:yyyy-MM-dd HH:mm} ");
                    textThe.Append(it.Name);
                    Verbose.Invoke(textThe.ToString());
                }
                else
                {
                    Verbose.Invoke(it.Name);
                }
                return it;
            })
            .Aggregate(
                seed: new SumZipEntry(ZipFilename ?? ".", isFile: true),
                func: (acc, it) => acc.AddWith(it)),
            resolve: (the, arg) =>
            {
                switch (arg)
                {
                    case "dir":
                        return (seq) => seq
                        .GroupBy((it) => Helper.GetFirstDirPart(it.Name))
                        .ToImmutableDictionary((grp) => grp.Key,
                        (grp) => grp.Aggregate(
                            seed: new SumZipEntry(grp.Key),
                            func: (acc, it) => acc.AddWith(it)))
                        .Select((grp) =>
                        {
                            Verbose.Invoke(grp.Value.ToString());
                            return grp.Value;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(ZipFilename ?? ".", isFile: true),
                            func: (acc, it) => acc.AddWith(it));
                    case "ext":
                        return (seq) => seq
                        .GroupBy((it) => Path.GetExtension(it.Name) ?? "*no-ext*")
                        .ToImmutableDictionary((grp) => grp.Key,
                        (grp) => grp.Aggregate(
                            seed: new SumZipEntry(grp.Key),
                            func: (acc, it) => acc.AddWith(it)))
                        .Select((grp) =>
                        {
                            Verbose.Invoke(grp.Value.ToString());
                            return grp.Value;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(ZipFilename ?? ".", isFile: true),
                            func: (acc, it) => acc.AddWith(it));
                    default:
                        throw new MyArgumentException(
                            $"'{arg}' is bad to {the.Name}");
                }
            });
}
