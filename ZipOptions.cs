namespace zip2;

static internal partial class My
{
    static internal string ZipFilename { get; private set; } = "";

    static internal IInvokeOption<bool, Stream> OpenZip
        = new SingleValueOption<bool, Stream>(
            "--file", shortcut: "-f",
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
            "--out-dir", shortcut: "-O",
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
}
