using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Archives.Rar;
using System.Collections.Immutable;
using System.Text;
using static zip2.Helper;
namespace zip2;

static internal partial class My
{
    static internal IInvokeOption<Non, bool> OtherColumnOpt
        = new NoValueOption<Non, bool>("--name-only",
            init: (_) => true, alt: (_) => false);

    static internal IInvokeOption<string, bool> IsFileFound
        = new SingleValueOption<string, bool>(
            "--if-find-on", shortcut: "-F", help: "DIR",
            init: (_) => true,
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                if (!Directory.Exists(arg))
                {
                    throw new MyArgumentException(
                        $"But {the.Name} dir '{arg}' is NOT found.");
                }
                return (path) => File.Exists(Path.Join(arg,
                    ToLocalFilename(path)));
            });

    static internal Func<IEnumerable<SumZipEntry>, IEnumerable<SumZipEntry>> SortSumEntry
    { get; private set; } = (seq) => seq;

    static internal IInvokeOption<IEnumerable<MyZipEntry>, IEnumerable<MyZipEntry>> SortBy
        = new SingleValueOption<IEnumerable<MyZipEntry>, IEnumerable<MyZipEntry>>(
            "--sort", help: "name | date | size | ratio | last | count",
            shortcut: "-o", init: (it) => it,
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                switch (arg)
                {
                    case "name":
                        SortSumEntry = (seq) => seq.OrderBy((it) => it.Name);
                        return (seq) => seq.OrderBy((it) => it.Name);
                    case "date":
                        SortSumEntry = (seq) => seq.OrderBy((it) => it.DateTime);
                        return (seq) => seq.OrderBy((it) => it.DateTime);
                    case "size":
                        SortSumEntry = (seq) => seq.OrderBy((it) => it.Size);
                        return (seq) => seq.OrderBy((it) => it.Size);
                    case "ratio":
                        SortSumEntry = (seq) => seq.OrderBy((it) =>
                        ReducePentCent(it.Size, it.CompressedSize));
                        return (seq) => seq.OrderBy((it) =>
                        ReducePentCent(it.Size, it.CompressedSize));
                    case "last":
                        SortSumEntry = (seq) => seq.OrderBy((it) => it.Last);
                        return null;
                    case "count":
                        SortSumEntry = (seq) => seq.OrderBy((it) => it.Count);
                        return null;
                    default:
                        throw new MyArgumentException(
                        $"'{arg}' is bad to {the.Name}");
                }
            });

    static internal IInvokeOption<IEnumerable<MyZipEntry>, SumZipEntry> SumZip
        = new SingleValueOption<IEnumerable<MyZipEntry>, SumZipEntry>(
            "--sum", help: "ext | dir",
            init: (seq) => seq
            .Invoke(My.SortBy.Invoke)
            .Select((it) =>
            {
                if (My.OtherColumnOpt.Invoke(Non.e))
                {
                    var textThe = new StringBuilder();
                    textThe.Append(it.IsCrypted ? '*' : ' ');
                    textThe.Append(
                        $"{ReducePentCent(it.Size, it.CompressedSize)} ");
                    textThe.Append($"{KiloSize(it.Size)} ");
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
                        .Select((pair) => pair.Value)
                        .Invoke(SortSumEntry)
                        .Select((it) =>
                        {
                            Verbose.Invoke(it.ToString());
                            return it;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(ZipFilename ?? ".", isFile: true),
                            func: (acc, it) => acc.AddWith(it));
                    case "ext":
                        return (seq) => seq
                        .Select((it) => (Path.GetExtension(it.Name), it))
                        .GroupBy((pair) => string.IsNullOrEmpty(pair.Item1)
                        ? "*no-ext*" : pair.Item1)
                        .ToImmutableDictionary((grp) => grp.Key,
                        (grp) => grp.Aggregate(
                            seed: new SumZipEntry(grp.Key),
                            func: (acc, it) => acc.AddWith(it.Item2)))
                        .Select((pair) => pair.Value)
                        .Invoke(SortSumEntry)
                        .Select((it) =>
                        {
                            Verbose.Invoke(it.ToString());
                            return it;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(ZipFilename ?? ".", isFile: true),
                            func: (acc, it) => acc.AddWith(it));
                    default:
                        throw new MyArgumentException(
                            $"'{arg}' is bad to {the.Name}");
                }
            });

    static internal
        IInvokeOption<Non, Func<IEnumerable<MyZipEntry>, IEnumerable<MyZipEntry>>>
        SelectStructure = new
        NoValueOption<Non, Func<IEnumerable<MyZipEntry>, IEnumerable<MyZipEntry>>>(
            "--dir-only",
            init: (_) => (seq) => seq, alt: (_) =>
            {
                var dirnames = new HashSet<string>();

                IEnumerable<MyZipEntry> GetDir(IEnumerable<MyZipEntry> seqThe)
                {
                    foreach (var entryThe in seqThe)
                    {
                        var dirThe = Path.GetDirectoryName(entryThe.Name);
                        dirnames.Add(string.IsNullOrEmpty(dirThe)
                            ? "." : dirThe);
                    }

                    foreach (var dirThe in dirnames)
                    {
                        Verbose.Invoke(dirThe);
                    }

                    return Enumerable.Empty<MyZipEntry>();
                }

                var optThe = (IOption)TotalText;
                optThe.Resolve(optThe,
                    new string[] { optThe.Name });

                optThe = (IOption)Verbose;
                optThe.Resolve(optThe,
                    new string[] { optThe.Name });

                return (seq) => GetDir(seq);
            });
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
        (IOption) My.IsFileFound,
        (IOption) My.SortBy,
        (IOption) My.SumZip,
        (IOption) My.SelectStructure,
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

        Func<MyZipEntry, bool> checkZipEntryName =
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

        var extThe = Path.GetExtension(My.ZipFilename).ToLower();

        IEnumerable<MyZipEntry> seqThe = (extThe) switch
        {
            ".zip" => new ZipInputStream(ins).MyZipEntries(),
            ".rar" => RarArchive.Open(ins, new SharpCompress.Readers
            .ReaderOptions() { LeaveStreamOpen = true }).MyRarEntries(),
            _ => throw new MyArgumentException(
                $"'{extThe}' is unknown extension"),
        };

        var sumThe = seqThe
            .Where((it) => checkZipEntryName(it))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Where((it) => My.IsFileFound.Invoke(it.Name))
            .Invoke(My.SelectStructure.Invoke(Non.e))
            .Invoke(My.SumZip.Invoke);
        ins.Close();
        My.TotalText.Invoke(sumThe.ToString());
        return true;
    }
}
