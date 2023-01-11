namespace zip2;

static internal partial class My
{
    static internal string ZipFilename { get; private set; } = "";

    static internal IInvokeOption<bool, Stream> OpenZip
        = new SingleValueOption<bool, Stream>(
            "--file", help: "ZIP-FILENAME", shortcut: "-f",
            init: (_) => throw new MyArgumentException(
                "Zip file is required by '--file' or '-f'."),
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                return (readonlyFlag) =>
                {
                    ZipFilename = arg;
                    if (readonlyFlag)
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

    static internal IInvokeOption<string, string> ToOutDir
        = new SingleValueOption<string, string>(
            "--out-dir", help: "OUTPUT-DIR", shortcut: "-O",
            init: (it) => it,
            resolve: (the, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return null;
                return (path) => Path.Combine(arg, path);
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
}
