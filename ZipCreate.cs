using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;

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

    internal record CreateFileParam(Stream Stream, IEnumerable<string> Paths);

    static internal readonly IInvokeOption<CreateFileParam, int>
        CreateFileFormat = new
        SingleValueOption<CreateFileParam, int>(
            "--format", help: "zip | tar | tgz",
            init: (param) =>
            {
                var nameThe = ZipFilename.ToLower();
                if (nameThe.EndsWith(".zip"))
                {
                    return Create.MakeZip(param);
                }
                if (nameThe.EndsWith(".tar"))
                {
                    return Create.MakeTar(param, isGzipCompressed: false);
                }
                if (nameThe.EndsWith(".tgz") || nameThe.EndsWith(".tar.gz"))
                {
                    return Create.MakeTar(param, isGzipCompressed: true);
                }
                throw new MyArgumentException($"File ext of '{ZipFilename}' is unknown!");
            },
            resolve: (the, arg) =>
            {
                switch (arg)
                {
                    case "zip":
                        return (param) => Create.MakeZip(param);
                    case "tar":
                        return (param) => Create.MakeTar(param
                            , isGzipCompressed: false);
                    case "tgz":
                        return (param) => Create.MakeTar(param
                            , isGzipCompressed: true);
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
        (IOption) My.Verbose,
        (IOption) My.TotalText,
        (IOption) My.CompressLevel,
        (IOption) My.FilesFrom,
        (IOption) My.CreateFileFormat,
        (IOption) My.OpenZip,
    }.ToImmutableArray();

    static readonly ImmutableDictionary<string, string[]> MyShortcutArrays =
        new Dictionary<string, string[]>
        {
            ["-0"] = new[] { "--level", "storage" },
            ["-1"] = new[] { "--level", "faster" },
            ["-2"] = new[] { "--level", "fast" },
            ["-3"] = new[] { "--level", "smallest" },
            ["-Z"] = new[] { "--format", "zip" },
            ["-A"] = new[] { "--format", "tar" },
            ["-G"] = new[] { "--format", "tgz" },
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

        (args, var outs, var close) = My.OpenZip.Invoke(new
            My.OpenZipParam(args, IsExisted: false));
        if (outs == Stream.Null)
        {
            Console.WriteLine("Create failed.");
            return false;
        }

        string shadowName = "?";
        if (Helper.Stdout != My.ZipFilename)
        {
            close(outs);
            shadowName = My.ZipFilename +
                $".{Guid.NewGuid()}.zip2.tmp";
            File.Move(My.ZipFilename, shadowName);
            outs = File.OpenWrite(shadowName);
        }

        var cntAdded = My.CreateFileFormat.Invoke(new My.CreateFileParam(
            outs, args.Concat(My.FilesFrom.Invoke(true))));

        close(outs);
        My.TotalText.Invoke($"Add OK:{cntAdded}");

        if (Helper.Stdout != My.ZipFilename)
        {
            File.Move(shadowName, My.ZipFilename);
        }

        if (1 > cntAdded && File.Exists(My.ZipFilename))
        {
            My.TotalText.Invoke($"Clean {My.ZipFilename}");
            File.Delete(My.ZipFilename);
        }
        return true;
    }

    static internal int MakeZip(My.CreateFileParam param)
    {
        int cntAdded = 0;
        var buffer1 = new byte[32 * 1024];
        var buffer2 = new byte[32 * 1024];
        var outZs = new ZipOutputStream(param.Stream);
        My.CompressLevel.Invoke(outZs);
        var zipFullPath = Path.GetFullPath(My.ZipFilename);
        foreach (var path in param.Paths
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

        return cntAdded;
    }

    static internal int MakeTar(My.CreateFileParam param,
        bool isGzipCompressed)
    {
        int cntAdded = 0;
        var buffer1 = new byte[32 * 1024];
        var buffer2 = new byte[32 * 1024];
        TarOutputStream tos;
        GZipStream? gzs = null;
        if (isGzipCompressed)
        {
            gzs = new GZipStream(param.Stream, CompressionMode.Compress);
            tos = new TarOutputStream(gzs, Encoding.UTF8)
            { IsStreamOwner = false };
        }
        else
        {
            tos = new TarOutputStream(param.Stream, Encoding.UTF8)
            { IsStreamOwner = false };
        }
        var targetFullPath = Path.GetFullPath(My.ZipFilename);
        foreach (var path in param.Paths
            .Select((it) => Helper.ToLocalFilename(it))
            .Where((it) => File.Exists(it))
            .Where((it) =>
            {
                var theFullpath = Path.GetFullPath(it);
                return (targetFullPath != theFullpath);
            })
            .Distinct())
        {
            var infoThe = new FileInfo(path);
            var a2 = TarEntry.CreateTarEntry(Helper.ToStandFilename(path));
            a2.Size = infoThe.Length;
            a2.ModTime = infoThe.LastWriteTime.ToUniversalTime();
            a2.GroupId = 201;
            a2.UserId = 101;
            a2.GroupName = "default";
            a2.UserName = "default";
            a2.TarHeader.Mode = 511; // 0o7777
            a2.TarHeader.TypeFlag = 48;
            tos.PutNextEntry(a2);

            long writtenSize = 0L;
            int readSize = 0;
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
                            taskWrite = tos.WriteAsync(buffer1, 0, readSize);
                        }
                        else
                        {
                            isBuffer1Read = true;
                            taskRead = fs.ReadAsync(buffer1, 0, buffer1.Length);
                            taskWrite = tos.WriteAsync(buffer2, 0, readSize);
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

            // TODO: if (sizeThe != writtenSize) ..
            tos.CloseEntry();
        }
        tos.Finish();
        tos.Close();
        gzs?.Close();

        return cntAdded;
    }
}
