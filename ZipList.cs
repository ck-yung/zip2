using System.Collections.Immutable;
namespace zip2;

[Command(name: "--list", shortcut: "-l", help: """
      zip2 -lf ZIP-FILE
      zip2 --list --file ZIP-FILE    
    """)]
public class List : ICommandMaker
{
    public MyCommand Make()
    {
        return new CommandThe();
    }

    static ImmutableArray<IOption> MyOptions = new IOption[]
    {
        (IOption) My.OpenZip,
    }.ToImmutableArray();

    class CommandThe : MyCommand
    {
        public CommandThe() : base(
            invoke: (args) => List.Invoke(args),
            options: MyOptions)
        { }
    }

    static bool Invoke(string[] args)
    {
        if (args.Contains("--help"))
        {
            Console.WriteLine(
                """
                List zip file:
                  zip2 -tf ZIP-FILE [OPTION ..]
                """);
            Helper.PrintHelp(MyOptions);
            return false;
        }

        var ins = My.OpenZip.Invoke(true);
        if (ins == Stream.Null)
        {
            Console.WriteLine("Open failed.");
            return false;
        }
        Console.WriteLine("Open succeeded.");
        ins.Close();
        return true;
    }
}
