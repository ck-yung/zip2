using SharpCompress.Archives.Rar;
using static zip2.Helper;

namespace zip2;

static internal partial class My
{
    static internal string ZipFilename { get; private set; } = "";

    internal record OpenZipParam(string[] Args, bool IsExisted = true);
    internal record OpenZipResult(string[] Args, Stream Stream);

    static OpenZipResult GetOpenZip(string zipFilename, IEnumerable<string> args, bool isExisted)
    {
        var gg = args
            .Select((it) => it.Split(','))
            .SelectMany((it) => it)
            .Where((it) => it.Length > 0)
            .Distinct();
        if (isExisted)
        {
            if (false == File.Exists(zipFilename))
            {
                var dir3 = Path.GetDirectoryName(zipFilename);
                if (string.IsNullOrEmpty(dir3)) dir3 = ".";
                if (dir3.EndsWith(':'))
                {
                    dir3 += '.';
                    dir3 += Path.DirectorySeparatorChar;
                }
                if (!Directory.Exists(dir3))
                {
                    throw new MyArgumentException($"Dir '{dir3}' is NOT found.");
                }
                var name3 = Path.GetFileName(zipFilename);
                if (string.IsNullOrEmpty(name3))
                {
                    throw new MyArgumentException($"'{zipFilename}' is NOT filename.");
                }
                var aa = Directory.GetFiles(dir3, name3);
                if ((aa == null) || (aa.Length == 0))
                {
                    throw new MyArgumentException(
                        $"No file is matched to '{zipFilename}'");
                }
                if (aa.Length > 1)
                {
                    throw new MyArgumentException(
                        $"{aa.Length} files are matched to '{zipFilename}'");
                }
                zipFilename = aa[0];
            }
            if (false == File.Exists(zipFilename))
            {
                throw new MyArgumentException(
                    $"But input '{zipFilename}' is NOT found!");
            }
            return new OpenZipResult(gg.ToArray(), File.OpenRead(zipFilename));
        }
        if (File.Exists(zipFilename))
            throw new MyArgumentException(
                $"But output '{zipFilename}' does EXIST!");
        return new OpenZipResult(gg.ToArray(), File.Create(zipFilename));
    }
    static internal IInvokeOption<OpenZipParam, OpenZipResult> OpenZip
        = new SingleValueOption<OpenZipParam, OpenZipResult>(
            "--file", help: "ZIP-FILENAME", shortcut: "-f",
            init: (it) =>
            {
                if (it.Args.Length == 0)
                    throw new ArgumentException($"Syntax: {nameof(zip2)} -?");
                ZipFilename = it.Args[0];
                return GetOpenZip(ZipFilename, it.Args.Skip(1), it.IsExisted);
            },
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg) || arg.Length == 0)
                    throw new ArgumentException($"Filename is required to '{the.Name}','{the.Shortcut}'");
                return (it) =>
                {
                    ZipFilename = arg;
                    return GetOpenZip(ZipFilename, it.Args, it.IsExisted);
                };
            });

    static internal readonly IInvokeOption<string, bool> ExclFiles
        = new ManyValuesOption<string, bool>("--excl", help: "WILD[,WILD,..]",
            init: (_) => false, resolve: (the, args) =>
            {
                var aa = args
                .Select((it) => it.Split(',',';'))
                .SelectMany((it) => it)
                .Where((it) => it.Length > 0)
                .Distinct()
                .ToArray();
                if (aa.Length == 0) return (_) => false;
                return Helper.ToWildPredicate(aa);
            });

    static internal IInvokeOption<bool, IEnumerable<string>> FilesFrom
        = new SingleValueOption<bool, IEnumerable<string>>(
            "--files-from", help: "FILES-FROM", shortcut: "-T",
            init: (_) => Enumerable.Empty<string>(),
            resolve: (the, arg) =>
            {
                IEnumerable<string> ReadNamesFrom(string fromFile)
                {
                    StreamReader filesFromStream;
                    if (fromFile == "-")
                    {
                        if (false == Console.IsInputRedirected)
                        {
                            throw new MyArgumentException(
                                "But console input is NOT redirected.");
                        }
                        filesFromStream = new StreamReader(Console.OpenStandardInput());
                    }
                    else
                    {
                        filesFromStream = File.OpenText(arg);
                    }

                    string? lineThe;
                    while (null != (lineThe = filesFromStream.ReadLine()))
                    {
                        yield return lineThe.Trim();
                    }
                }

                if (false == File.Exists(arg) && arg != "-")
                {
                    throw new MyArgumentException(
                        $"But files-file '{arg}' is NOT found!");
                }

                return (_) => ReadNamesFrom(arg);
            });

    static internal IInvokeOption<string, Non> Verbose
        = new NoValueOption<string, Non>(
            "--verbose", shortcut: "-v",
            alt: (msg) =>
            {
                Console.WriteLine(msg);
                return Non.e;
            },
            init: (msg) => Non.e);

    static internal IInvokeOption<string, Non> LogError
        = new NoValueOption<string, Non>(
            "--quiet", shortcut: "-q",
            init: (msg) =>
            {
                Console.Error.WriteLine(msg);
                return Non.e;
            },
            alt: (msg) => Non.e);

    static internal IInvokeOption<string, Non> TotalText
        = new NoValueOption<string, Non>(
            "--total-off",
            init: (msg) =>
            {
                Console.WriteLine(msg);
                return Non.e;
            },
            alt: (msg) => Non.e);

    static internal string LastRarArchivePath { set; get; } = "?";
    internal record GetRarEntriesParam(Stream Stream, string Path);
    static internal IInvokeOption<GetRarEntriesParam, IEnumerable<MyZipEntry>> GetRarEntries
        = new NoValueOption<GetRarEntriesParam, IEnumerable<MyZipEntry>>(
            "--multi-vol", shortcut: "-m",
            init: (arg) => RarArchive.Open(arg.Stream)
            .Entries
            .Where((it) => false == it.IsDirectory)
            .Select((it) => new MyZipEntry(it)),
            alt: (arg) =>
            {
                int ndx = 1;
                IEnumerable<Stream> GetInputStreams(Stream first)
                {
                    yield return first;
                    while (true)
                    {
                        ndx += 1;
                        LastRarArchivePath = arg.Path.Replace("part1", $"part{ndx}");
                        if (LastRarArchivePath.Equals(arg.Path))
                            throw new MyArgumentException(
                                $"'{arg.Path}' does NOT contains 'part1' !");

                        if (false == File.Exists(LastRarArchivePath)) yield break;
                        yield return File.OpenRead(LastRarArchivePath);
                    }
                }
                return RarArchive.Open(GetInputStreams(arg.Stream))
                .Entries.Where((it) => false == it.IsDirectory)
                .Select((it) => new MyZipEntry(it));
            });
}
