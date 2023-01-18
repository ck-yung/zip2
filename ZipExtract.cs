using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2;

static internal partial class My
{
    static internal IInvokeOption<bool, Func<string, string>> ToOutDir
        = new SingleValueOption<bool, Func<string, string>>(
            "--out-dir", help: "OUTPUT-DIR", shortcut: "-O",
            init: (_) => (it) => it,
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                return (_) =>
                {
                    if (arg == "-")
                    {
                        arg = Path.GetFileNameWithoutExtension(
                            ZipFilename) ?? string.Empty;
                        if (string.IsNullOrEmpty(arg))
                            throw new MyArgumentException("But --file is NOT set!");
                        return (path) => Path.Combine(arg, path);
                    }
                    return (path) => Path.Combine(arg, path);
                };
            });

    static internal IInvokeOption<string, Stream> Overwrite
        = new NoValueOption<string, Stream>(
            "--overwrite", shortcut: "-o",
            init: (path) =>
            {
                if (File.Exists(path))
                    return Stream.Null;
                return File.Create(path);
            },
            alt: (path) => File.Create(path));

}

[Command(name: "--extract", shortcut: "-x", help: """
      zip2 -xf ZIP-FILE [OPTION ..] [WILD ..]
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
        (IOption) My.Verbose,
        (IOption) My.TotalText,
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
        if (args.Contains(CommandMaker.HelpText))
        {
            Console.WriteLine(
                """
                Extract zip file:
                  zip2 -xf ZIP-FILE [OPTION ..] [WILD ..]

                Output dir will be 'ZIP-FILE' if '--out-dir -'
                """);
            Helper.PrintHelp(MyOptions);
            return false;
        }

        var ins = My.OpenZip.Invoke((true, "'zip2 -x?' for help"));
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

        var buffer1 = new byte[32 * 1024];
        var buffer2 = new byte[32 * 1024];

        var inpZs = new ZipInputStream(ins);
        var toOutDir = My.ToOutDir.Invoke(true);
        var count = List.GetEntries(inpZs)
            .Where((it) => it.IsFile)
            .Where((it) => checkZipEntryName(it))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Select((it) =>
            {
                var targetFilename = toOutDir(
                    Helper.ToLocalFilename(it.Name));
                var dirThe = Path.GetDirectoryName(targetFilename);
                if (!string.IsNullOrEmpty(dirThe))
                {
                    Directory.CreateDirectory(dirThe);
                }
                var outs = My.Overwrite.Invoke(targetFilename);
                if (outs == Stream.Null)
                {
                    My.Verbose.Invoke($"Skip existing {targetFilename}");
                    return false;
                }
                My.Verbose.Invoke(targetFilename);
                if (1 > it.Size)
                {
                    outs.Close();
                    return true;
                }

                int readSize = 0;
                bool isBuffer1Read = true;
                var taskRead = inpZs.ReadAsync(buffer1, 0, buffer1.Length);
                var taskWrite = Stream.Null.WriteAsync(buffer2, 0, 0);
                while (true)
                {
                    taskWrite.Wait();
                    taskRead.Wait();
                    readSize = taskRead.Result;
                    if (1 > readSize) break;

                    if (isBuffer1Read)
                    {
                        isBuffer1Read = false;
                        taskRead = inpZs.ReadAsync(buffer2, 0, buffer2.Length);
                        taskWrite = outs.WriteAsync(buffer1, 0, readSize);
                    }
                    else
                    {
                        isBuffer1Read = true;
                        taskRead = inpZs.ReadAsync(buffer1, 0, buffer1.Length);
                        taskWrite = outs.WriteAsync(buffer2, 0, readSize);
                    }
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
        My.TotalText.Invoke($"Extract OK:{count}");
        return true;
    }
}
