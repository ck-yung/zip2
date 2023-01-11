using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;
using System.Text;

namespace zip2;

[Command(name: "--list", shortcut: "-t", help: """
      zip2 -tf ZIP-FILE
      zip2 --list --file ZIP-FILE
    """)]
public class List : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.ExclFiles,
        (IOption) My.SumZip,
    }.ToImmutableArray();

    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => List.Invoke(args),
            options: MyOptions)
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
        if (args.Contains("--help"))
        {
            Console.WriteLine(
                """
                List zip file:
                  zip2 -tf ZIP-FILE [OPTION ..]
                """);
            Helper.PrintHelp(MyOptions);
            return false;
        }

        var wildNames = Helper.ToWildPredicate(args);
        var ins = My.OpenZip.Invoke(true);
        if (ins == Stream.Null)
        {
            Console.WriteLine("Open failed.");
            return false;
        }

        var inpZs = new ZipInputStream(ins);
        var sumThe = GetEntries(inpZs)
            .Where((it) => wildNames(Path.GetFileName(it.Name)))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Invoke(My.SumZip.Invoke);
        ins.Close();
        Console.WriteLine(sumThe.ToString());
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
    public bool IsCrypted { get; private set; } = false;
    public int Count { get; private set; } = 0;
    public long Size { get; private set; } = 0;
    public DateTime DateTime { get; private set; } = DateTime.MaxValue;
    public DateTime Last { get; private set; } = DateTime.MinValue;

    public override string ToString()
    {
        var rtn = new StringBuilder();
        rtn.Append(IsCrypted ? '*' : ' ');
        rtn.Append($"{List.KiloSize(Size)} ");
        rtn.Append($"{DateTime:yyyy-MM-dd HH:mm} ");
        rtn.Append($"- ");
        rtn.Append($"{Last:yyyy-MM-dd HH:mm} ");
        rtn.Append($"{Count,5:N0} ");
        rtn.Append(Name);
        return rtn.ToString();
    }

    public SumZipEntry AddWith(ZipEntry entryThe)
    {
        Count += 1;
        Size += entryThe.Size;
        if (DateTime > entryThe.DateTime) DateTime = entryThe.DateTime;
        if (Last < entryThe.DateTime) Last = entryThe.DateTime;
        if (entryThe.IsCrypted) IsCrypted = true;
        return this;
    }

    public SumZipEntry AddWith(SumZipEntry other)
    {
        Count += other.Count;
        Size += other.Size;
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
                Console.Write(it.IsCrypted ? '*' : ' ');
                Console.Write($"{List.KiloSize(it.Size)} ");
                Console.Write($"{it.DateTime:yyyy-MM-dd HH:mm} ");
                Console.WriteLine(it.Name);
                return it;
            })
            .Aggregate(
                seed: new SumZipEntry(Path.GetFileName(My.ZipFilename) ?? "."),
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
                            Console.WriteLine(grp.Value.ToString());
                            return grp.Value;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(Path.GetFileName(My.ZipFilename) ?? "."),
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
                            Console.WriteLine(grp.Value.ToString());
                            return grp.Value;
                        })
                        .Aggregate(
                            seed: new SumZipEntry(Path.GetFileName(My.ZipFilename) ?? "."),
                            func: (acc, it) => acc.AddWith(it));
                    default:
                        throw new MyArgumentException(
                            $"'{arg}' is bad to {the.Name}");
                }
            });
}
