using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace zip2;

static internal partial class My
{
    static internal readonly IInvokeOption<bool, Func<string, string>> ToOutDir
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

    static internal readonly IInvokeOption<(string, string), Stream>
        CreateExtractFile
        = new NoValueOption<(string, string), Stream>(
            "--overwrite", shortcut: "-o",
            init: (arg) =>
            {
                if (File.Exists(arg.Item1)) return Stream.Null;
                return File.Create(arg.Item2);
            },
            alt: (arg) =>
            {
                if (File.Exists(arg.Item1)) File.Delete(arg.Item1);
                return File.Create(arg.Item2);
            });

    static internal readonly IInvokeOption<string, string> PathNameOpt
        = new NoValueOption<string, string>(
            "--no-dir", init: (path) => path,
            alt: (path) => Path.GetFileName(path));

    internal record SetFileTimeParam(string Path,
        DateTime LastWrite, DateTime Creation);

    static internal Func<SetFileTimeParam, bool> InitSetLastWriteTime()
    {
        var rid = RuntimeInformation.RuntimeIdentifier.ToLower();
        if (rid.StartsWith("win") || rid.StartsWith("osx"))
        {
            return (arg) =>
            {
                File.SetLastWriteTime(arg.Path, arg.LastWrite);
                File.SetCreationTime(arg.Path, arg.Creation);
                return true;
            };
        }
        return (arg) =>
        {
            File.SetLastWriteTime(arg.Path, arg.LastWrite);
            return true;
        };
    }

    static internal readonly IInvokeOption<SetFileTimeParam, bool>
        UpdateLastWriteTimeOpt
        = new NoValueOption<SetFileTimeParam, bool>("--no-time",
            init: InitSetLastWriteTime(),
            alt: (_) => { return false; });
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

    static readonly ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.Verbose,
        (IOption) My.LogError,
        (IOption) My.TotalText,
        (IOption) My.PathNameOpt,
        (IOption) My.UpdateLastWriteTimeOpt,
        (IOption) My.ToOutDir,
        (IOption) My.CreateExtractFile,
        (IOption) My.ExclFiles,
        (IOption) My.FilesFrom,
        (IOption) My.OpenCompressedFile,
    }.ToImmutableArray();

    static readonly ImmutableDictionary<string, string[]> MyShortcuts =
        new Dictionary<string, string[]>
        {
            ["-b"] = new string[] {
                "--verbose", "--name-only", "--total-off"},
            ["-Z"] = new string[] { "--format", "zip" },
            ["-R"] = new string[] { "--format", "rar" },
        }.ToImmutableDictionary();

    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => Extract.Invoke(args),
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
                Extract zip file:
                  zip2 -xf ZIP-FILE [OPTION ..] [WILD ..]

                Output dir will be 'ZIP-FILE' if '--out-dir -'
                """);
            Helper.PrintHelp(MyOptions,
                shortcutArrays: MyShortcuts);
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

        Func<MyZipEntry, bool> checkZipEntryName =
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
        var toOutDir = My.ToOutDir.Invoke(true);

        var creationTime = DateTime.Now;
        string tmpExt = $".{Guid.NewGuid()}.zip2.tmp";
        var count = My.OpenCompressedFile.Invoke((ins, My.ZipFilename))
            .Where((it) => it.IsFile)
            .Where((it) => checkZipEntryName(it))
            .Where((it) => false == My.ExclFiles.Invoke(
                Path.GetFileName(it.Name)))
            .Select((it) =>
            {
                var targetFilename = toOutDir(
                    Helper.ToLocalFilename(
                        My.PathNameOpt.Invoke(it.Name)));
                var dirThe = Path.GetDirectoryName(targetFilename);
                if (!string.IsNullOrEmpty(dirThe))
                {
                    Directory.CreateDirectory(dirThe);
                }
                var targetShadowName = targetFilename + tmpExt;
                var outs = My.CreateExtractFile.Invoke(
                    (targetFilename, targetShadowName));
                if (outs == Stream.Null)
                {
                    My.LogError.Invoke($"Skip existing {targetFilename}");
                    return false;
                }
                My.Verbose.Invoke(targetFilename);
                if (1 > it.Size)
                {
                    outs.Close();
                    return true;
                }

                var entryStream = it.OpenStream();
                int readSize = 0;
                bool isBuffer1Read = true;
                var taskRead = entryStream.ReadAsync(buffer1, 0, buffer1.Length);
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
                        taskRead = entryStream.ReadAsync(buffer2, 0, buffer2.Length);
                        taskWrite = outs.WriteAsync(buffer1, 0, readSize);
                    }
                    else
                    {
                        isBuffer1Read = true;
                        taskRead = entryStream.ReadAsync(buffer1, 0, buffer1.Length);
                        taskWrite = outs.WriteAsync(buffer2, 0, readSize);
                    }
                }
                it.CloseStream(entryStream);
                outs.Close();

                if (File.Exists(targetShadowName))
                {
                    My.UpdateLastWriteTimeOpt.Invoke(
                        new My.SetFileTimeParam(targetShadowName,
                        it.DateTime, creationTime));
                    File.Move(targetShadowName, targetFilename);
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
