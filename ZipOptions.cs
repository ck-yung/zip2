using static zip2.Helper;

namespace zip2;

static internal partial class My
{
    static internal string ZipFilename { get; private set; } = "";

    static internal IInvokeOption<(bool,string), Stream> OpenZip
        = new SingleValueOption<(bool, string), Stream>(
            "--file", help: "ZIP-FILENAME", shortcut: "-f",
            init: (it) => throw new MyArgumentException(
                "Zip file is required by '--file' or '-f'." +
                Environment.NewLine + it.Item2),
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                return (it) =>
                {
                    ZipFilename = arg;
                    if (it.Item1)
                    {
                        if (false == File.Exists(arg))
                            throw new MyArgumentException(
                                $"But input '{arg}' is NOT found!");
                        return File.OpenRead(arg);
                    }
                    if (File.Exists(arg))
                        throw new MyArgumentException(
                            $"But output '{arg}' does EXIST!");
                    return File.Create(arg);
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
}
