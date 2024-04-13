using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2;

static internal partial class My
{
    static internal readonly IInvokeOption<ZipOutputStream, int> CompressLevel
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
      zip2 -cf NEW-ZIP [OPTION ..] FILE [FILE ..]
    """)]
public class Create : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static readonly ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
        (IOption) My.Verbose,
        (IOption) My.TotalText,
        (IOption) My.CompressLevel,
        (IOption) My.FilesFrom,
    }.ToImmutableArray();

    static readonly ImmutableDictionary<string, string[]> MyShortcutArrays =
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
        if (args.Contains(CommandMaker.HelpText))
        {
            Console.WriteLine(
                """
                Create zip file:
                  zip2 -cf NEW-ZIP [OPTION ..] FILE [FILE ..]
                  zip2 -cf NEW-ZIP [OPTION ..] -T - [FILE ..]
                For example,
                  dir2 src -bsk --within 2hour | zip2 -c3f ../today.zip -T -

                """);
            Helper.PrintHelp(MyOptions, MyShortcutArrays);
            return false;
        }

        var ins = My.OpenZip.Invoke(new My.OpenZipParam("'zip2 -c?' for help", IsExisted: false));
        if (ins == Stream.Null)
        {
            Console.WriteLine("Create failed.");
            return false;
        }

        ins.Close();
        var extThe = Path.GetExtension(My.ZipFilename).ToLower();
        if (extThe != ".zip")
        {
            File.Delete(My.ZipFilename);
            throw new MyArgumentException(
                $"Ext should be '.zip' but '{extThe}' is found!");
        }

        string shadowName = My.ZipFilename +
            $".{Guid.NewGuid()}.zip2.tmp";
        File.Move(My.ZipFilename, shadowName);
        ins = File.OpenWrite(shadowName);

        int cntAdded = 0;
        var buffer1 = new byte[32 * 1024];
        var buffer2 = new byte[32 * 1024];
        var outZs = new ZipOutputStream(ins);
        My.CompressLevel.Invoke(outZs);
        var zipFullPath = Path.GetFullPath(My.ZipFilename);
        foreach (var path in args.Concat(My.FilesFrom.Invoke(true))
            .Select((it) => Helper.ToLocalFilename(it))
            .Where((it) => File.Exists(it))
            .Where((it) =>
            {
                var theFullpath = Path.GetFullPath(it);
                return (zipFullPath != theFullpath);
            })
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
            My.Verbose.Invoke(path);
            byte[] buffer = new byte[32 * 1024];
            using (FileStream fs = File.OpenRead(path))
            {
                try
                {
                    bool isBuffer1Read = true;
                    var taskRead = fs.ReadAsync(buffer1, 0, buffer1.Length);
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
                            taskRead = fs.ReadAsync(buffer2, 0, buffer2.Length);
                            taskWrite = outZs.WriteAsync(buffer1, 0, readSize);
                        }
                        else
                        {
                            isBuffer1Read = true;
                            taskRead = fs.ReadAsync(buffer1, 0, buffer1.Length);
                            taskWrite = outZs.WriteAsync(buffer2, 0, readSize);
                        }
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
            outZs.CloseEntry();
        }
        outZs.Finish();
        outZs.Close();
        ins.Close();
        My.TotalText.Invoke($"Add OK:{cntAdded}");

        File.Move(shadowName, My.ZipFilename);
        if (1 > cntAdded && File.Exists(My.ZipFilename))
        {
            My.TotalText.Invoke($"Clean {My.ZipFilename}");
            File.Delete(My.ZipFilename);
        }
        return true;
    }
}
