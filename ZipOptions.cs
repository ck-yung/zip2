namespace zip2;

static internal partial class My
{
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
}
