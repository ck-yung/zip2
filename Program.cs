namespace zip2;

class Program
{
    static public void Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    $"""
                    For more info, please run
                      {Helper.ExeName} -?
                    """);
                return;
            }
            RunMain(args);
        }
        catch (MyArgumentException ae)
        {
            Console.WriteLine(ae.Message);
        }
        catch (SharpCompress.Common.MultiVolumeExtractionException)
        {
            IOption opt = (IOption)My.GetRarEntries;
            Console.WriteLine($"Option {opt.Name} (shortcut '{opt.Shortcut}') is required!");
        }
        catch (SharpCompress.Common.IncompleteArchiveException)
        {
            Console.WriteLine($"IncompleteArchive! '{My.LastRarArchivePath}' is NOT found!");
        }
        catch (Exception ee)
        {
            if (Helper.GetExeEnvr().Contains(":dump-stack:"))
            {
                Console.WriteLine(My.ZipFilename ?? "*undefine-zipfile*");
                Console.WriteLine(ee);
            }
            else
            {
                if (string.IsNullOrEmpty(My.ZipFilename))
                {
                    Console.WriteLine($"{ee.GetType()}: {ee.Message}");
                }
                else
                {
                    Console.WriteLine(
                        $"File: '{My.ZipFilename}'; {ee.GetType()}: {ee.Message}");
                }
            }
        }
    }


    static public bool RunMain(string[] args)
    {
        if (args.Length == 1)
        {
            var argThe = args[0];
            if (argThe.Length > 0 &&
                argThe[0] != '-')
            {
                args = ["-tvf", argThe];
            }
        }

        args = CommandMaker.Parse(args,
            out MyCommand commandThe);

        if (commandThe.IsFake())
        {
            Console.WriteLine(
                $"""
                No command is found.
                For more info, please run
                  {Helper.ExeName} -?
                """);
            return false;
        }

        args = commandThe.Parse(args
            .AsEnumerable()
            .Select((it) => new FlagedArg(false, it))
            )
            .Select((it) => it.Arg)
            .ToArray();
        return commandThe.Invoke(args);
    }
}
