using static zip2.Helper;

namespace zip2;

[Command(name: "--version", shortcut: "-V", help: """
      zip2 --version
    """)]
public class Version : ICommandMaker
{
    public MyCommand Make()
    {
        return new MyCommand(invoke: (_) =>
        {
            Console.WriteLine(
                $"{ExeName} v{ExeVersion} {ExeCopyright}");
            return true;
        });
    }
}
