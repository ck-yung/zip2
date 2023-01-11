using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2;

static internal partial class My
{
    static internal IInvokeOption<ZipOutputStream, int> CompressLevel
        = new SingleValueOption<ZipOutputStream, int>(
            "--level", help: "storage | faster | fast | smallest",
            init: (it) =>
            {
                it.SetLevel(5);
                return 5;
            },
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                switch (arg)
                {
                    case "storage":
                        return (it) =>
                        {
                            it.SetLevel(0);
                            return 0;
                        };
                    case "faster":
                        return (it) =>
                        {
                            it.SetLevel(3);
                            return 3;
                        };
                    case "fast":
                        return (it) =>
                        {
                            it.SetLevel(6);
                            return 6;
                        };
                    case "smallest":
                        return (it) =>
                        {
                            it.SetLevel(9);
                            return 9;
                        };
                    default:
                        throw new MyArgumentException(
                            $"'{arg}' is bad to '{the.Name}'");
                }
            });
}

[Command(name: "--create", shortcut: "-c", help: """
      zip2 -cf NEW-ZIP
      zip2 --create --file NEW-ZIP
    """)]
public class Create : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.CompressLevel,
        (IOption) My.FilesFrom,
    }.ToImmutableArray();

    static ImmutableDictionary<string, string[]> MyShortcutArrays =
        new Dictionary<string, string[]>
        {
            ["-0"] = new[] { "--level", "storage" },
            ["-1"] = new[] { "--level", "faster" },
            ["-2"] = new[] { "--level", "fast" },
            ["-3"] = new[] { "--level", "smallest" },
        }.ToImmutableDictionary();

    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => Create.Invoke(args),
            options: MyOptions,
            shortcutArrays: MyShortcutArrays)
        { }
    }

    static bool Invoke(string[] args)
    {
        if (args.Contains("--help"))
        {
            Console.WriteLine(
                """
                create zip file:
                  zip2 -cf NEW-ZIP [OPTION ..]
                """);
            Helper.PrintHelp(MyOptions, MyShortcutArrays);
            return false;
        }

        if (args.Length== 0)
        {
            Console.WriteLine("No files are require to be compressed.");
            return false;
        }

        var ins = My.OpenZip.Invoke(false);
        if (ins == Stream.Null)
        {
            Console.WriteLine("Create failed.");
            return false;
        }

        int cntAdded = 0;
        var outZs = new ZipOutputStream(ins);
        My.CompressLevel.Invoke(outZs);
        foreach (var path in args.Concat(My.FilesFrom.Invoke(true))
            .Select((it) => Helper.ToLocalFilename(it))
            .Where((it) => File.Exists(it))
            .Distinct())
        {
            var sizeThe = (new FileInfo(path)).Length;
            var entry = new ZipEntry(Helper.ToStandFilename(path))
            {
                DateTime = File.GetLastWriteTime(path),
                Size = sizeThe,
            };

            long writtenSize = 0L;
            int readSize = 0;
            outZs.PutNextEntry(entry); // TODO: isTranscational: true
            byte[] buffer = new byte[32 * 1024];
            using (FileStream fs = File.OpenRead(path))
            {
                try
                {
                    while (true)
                    {
                        readSize = fs.Read(buffer, 0, buffer.Length);
                        if (1 > readSize) break;
                        outZs.Write(buffer, 0, readSize);
                        writtenSize += readSize;
                    }
                    cntAdded += 1;
                }
                catch (ZipException zipEe)
                {
                    Console.Error.WriteLine(zipEe.Message);
                }
                catch (Exception ee)
                {
                    Console.Error.WriteLine(ee.Message);
                }
            }

            // TODO: if (sizeThe != writtenSize) outZs.Rollback();
            Console.WriteLine(path);
            outZs.CloseEntry();
        }
        outZs.Finish();
        outZs.Close();
        ins.Close();
        Console.WriteLine($"#ok:{cntAdded}");
        if (1>cntAdded && File.Exists(My.ZipFilename))
        {
            Console.WriteLine($"Clean {My.ZipFilename}");
            File.Delete(My.ZipFilename);
        }
        return true;
    }
}
