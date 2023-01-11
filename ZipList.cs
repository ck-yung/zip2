namespace zip2;

[Command(name: "--list", shortcut: "-l", help: """
      zip2 -lf ZIP-FILE
      zip2 --list --file ZIP-FILE    
    """)]
public class List : ICommandMaker
{
    public MyCommand Make()
    {
        return new MyCommand(invoke: (_) =>
        {
            Console.WriteLine("Would list the content of a zip file.");
            return true;
        });
    }
}
