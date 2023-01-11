using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2;

[Command(name: "--extract", shortcut: "-x", help: """
      zip2 -xf ZIP-FILE
      zip2 --extract --file ZIP-FILE
    """)]
public class Extract : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.ToOutDir,
        (IOption) My.Overwrite,
        (IOption) My.ExclFiles,
        (IOption) My.FilesFrom,
    }.ToImmutableArray();

    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => Extract.Invoke(args),
            options: MyOptions)
        { }
    }

    static bool Invoke(string[] args)
    {
        if (args.Contains("--help"))
        {
            Console.WriteLine(
                """
                Extract zip file:
                  zip2 -xf ZIP-FILE [OPTION ..]
                """);
            Helper.PrintHelp(MyOptions);
            return false;
        }

        var ins = My.OpenZip.Invoke(true);
        if (ins == Stream.Null)
        {
            Console.WriteLine("Open failed.");
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
                ( > 0, 0) => (it) => wildNames(Path.GetFileName(it.Name)),
                (0, > 0) => (it) => namesFromFile.Contains(it.Name),

                _ => (it) => namesFromFile.Contains(it.Name) ||
                wildNames(Path.GetFileName(it.Name)),
            };

        var inpZs = new ZipInputStream(ins);
        var count = List.GetEntries(inpZs)
            .Where((it) => it.IsFile)
            .Where((it) => checkZipEntryName(it))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Select((it) =>
            {
                var targetFilename = My.ToOutDir.Invoke(
                    Helper.ToLocalFilename(it.Name));
                var dirThe = Path.GetDirectoryName(targetFilename);
                if (!string.IsNullOrEmpty(dirThe))
                {
                    Directory.CreateDirectory(dirThe);
                }
                var outs = My.Overwrite.Invoke(targetFilename);
                if (outs == Stream.Null)
                {
                    Console.WriteLine($"Skip existing {targetFilename}");
                    return false;
                }
                Console.WriteLine(targetFilename);

                long wantSize = it.Size;
                int readSize = 0;
                var buffer = new byte[32 * 1024];
                while (wantSize > 0)
                {
                    if (wantSize > buffer.Length)
                    {
                        readSize = buffer.Length;
                    }
                    else
                    {
                        readSize = (int)wantSize;
                    }
                    readSize = inpZs.Read(buffer, 0, readSize);
                    if (1 > readSize) break;
                    outs.Write(buffer, 0, readSize);
                    wantSize -= readSize;
                }
                outs.Close();

                if (File.Exists(targetFilename))
                {
                    File.SetLastWriteTime(targetFilename, it.DateTime);
                }
                return true;
            })
            .Where(it => true == it)
            .Count();
        ins.Close();
        Console.WriteLine($"#ok:{count}");
        return true;
    }
}
