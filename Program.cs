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
        catch (Exception ee)
        {
            if (Helper.GetExeEnvr().Contains(":dump-stack:"))
            {
                Console.WriteLine(ee);
            }
            else
            {
                Console.WriteLine($"{ee.GetType()}: {ee.Message}");
            }
        }
    }

    static public bool RunMain(string[] args)
    {
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
            .Select((it) => (false, it)))
            .Select((it) => (it.Item2))
            .ToArray();
        return commandThe.Invoke(args);
    }
}
